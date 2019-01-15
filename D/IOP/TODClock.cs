/*
    BSD 2-Clause License

    Copyright Vulcan Inc. 2017-2018 and Living Computer Museum + Labs 2018
    All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and/or other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
    OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


using System;
using System.Threading;

namespace D.IOP
{
    public enum TODPowerUpSetMode
    {
        HostTimeY2K = 0,
        HostTime,
        SpecificDateAndTime,
        SpecificDate,
        NoChange,
    }

    public enum TODAccessMode
    {
        Read = 0x4,
        Clear = 0x2,
        Set = 0x1,
        None = 0,
    }

    public enum TODClockType
    {
        Read = 0,
        SetA,
        SetB,
        SetC,
        SetD,
    }

    /// <summary>
    /// Implements the Star's TOD clock and timing logic.
    /// The Star does not use an off-the-shelf RTC chip, just a series of
    /// 74LS393 dual 4-bit counters and 74LS165 shift registers 
    /// clocked off of a 1Hz clock -- these form a single 32-bit register
    /// that counts seconds.  It can be read and written by the IOP.
    /// 
    /// This uses a Timer to provide decent real-time clock interrupts
    /// independent of the speed of the running emulation.
    /// </summary>
    public class TODClock
    {
        public TODClock()
        {
            _lock = new ReaderWriterLockSlim();            

            // Get the timer rolling, it will tick once a second forever.
            // TODO: this is not particularly accurate, timekeeping-wise.
            _timer = new Timer(TimerTick, null, 1000, 1000);            

            Reset();
        }

        public void Reset()
        {
            _lock.EnterWriteLock();
                _interrupt = false;
            _lock.ExitWriteLock();

            _todReadBit = 0;
            _mode = TODAccessMode.None;
            _powerLoss = false;

            PowerUpSetMode = Configuration.TODSetMode;
            PowerUpSetTime = (PowerUpSetMode == TODPowerUpSetMode.SpecificDate) ? 
                Configuration.TODDate : Configuration.TODDateTime;

            SetTODClockInternal();
        }

        /// <summary>
        /// Resets the clock to the current value specified by the
        /// system configuration.
        /// </summary>
        public void ResetTODClockTime()
        {
            SetTODClockInternal();
        }

        /// <summary>
        /// How to set the TOD clock when the system is started.
        /// </summary>
        public TODPowerUpSetMode PowerUpSetMode;

        /// <summary>
        /// A specific time to set the TOD clock to
        /// </summary>
        public DateTime PowerUpSetTime;

        /// <summary>
        /// The Interrupt flag indicates that a (soft) TOD interrupt
        /// has occurred -- this is raised every time the clock ticks.
        /// </summary>
        public bool Interrupt
        {
            get
            {
                bool value;

                _lock.EnterReadLock();
                    value = _interrupt;
                _lock.ExitReadLock();
                return value;
            }
        }

        public bool PowerLoss
        {
            get
            {
                return _powerLoss;
            }
        }

        public void ClearInterrupt()
        {
            _lock.EnterWriteLock();
                _interrupt = false;
            _lock.ExitWriteLock();
        }

        public void SetMode(TODAccessMode mode)
        {
            _mode = mode;

            switch(mode)
            {
                case TODAccessMode.Read:
                    _todReadBit = 0;
                    break;

                case TODAccessMode.Set:
                    // TODO: anything need to be done here?
                    break;

                case TODAccessMode.Clear:
                    _todValue = 0;
                    break;
            }
        }

        public int ReadClockBit()
        {
            _lock.EnterReadLock();            
                // From BookKeepingTask.asm: "Bits from clock come in true, and most significant bit first."
                int value = (_todValue & (0x80000000 >> (_todReadBit & 0x1f))) != 0 ? 0x40 : 0;
            _lock.ExitReadLock();
            return value;
        }

        public void ClockBit(TODClockType type)
        {
            switch(type)
            {
                case TODClockType.Read:
                    _todReadBit++;
                    break;

                //
                // A clock to A,B,C or D causes the value
                // in the corresponding byte of the tod counter to be incremented.
                // A is the least-significant byte, D is the most-significant.
                //
                case TODClockType.SetA:
                    _lock.EnterWriteLock();
                        _todValue = ((_todValue & 0xffffff00) | ((_todValue + 1) & 0xff));
                        _powerLoss = false;
                    _lock.ExitWriteLock();
                    break;

                case TODClockType.SetB:
                    _lock.EnterWriteLock();
                        _todValue = ((_todValue & 0xffff00ff) | ((_todValue + 0x100) & 0xff00));
                        _powerLoss = false;
                    _lock.ExitWriteLock();
                    break;

                case TODClockType.SetC:
                    _lock.EnterWriteLock();
                        _todValue = ((_todValue & 0xff00ffff) | ((_todValue + 0x10000) & 0xff0000));
                        _powerLoss = false;
                    _lock.ExitWriteLock();
                    break;

                case TODClockType.SetD:
                    _lock.EnterWriteLock();
                        _todValue = ((_todValue & 0x00ffffff) | ((_todValue + 0x1000000) & 0xff000000));
                        _powerLoss = false;
                    _lock.ExitWriteLock();
                    break;

                default:
                    throw new NotImplementedException("Clock bit not implemented.");
            }
        }       

        private void SetTODClockInternal()
        {
            _lock.EnterWriteLock();
            switch (PowerUpSetMode)
            {
                case TODPowerUpSetMode.HostTimeY2K:
                    //
                    // Get current time, and move date back 28 years since most Star software isn't happy with Y2K.
                    // This keeps the calendar in sync at least.
                    //
                    _todValue = GetXeroxTime(DateTime.Now.ToLocalTime().AddYears(-28));
                    break;

                case TODPowerUpSetMode.HostTime:
                    //
                    // Set TOD clock to the current wall-clock time.
                    //
                    _todValue = GetXeroxTime(DateTime.Now.ToLocalTime());
                    break;

                case TODPowerUpSetMode.SpecificDateAndTime:
                    //
                    // Set TOD clock to the specified date and time.
                    //
                    _todValue = GetXeroxTime(PowerUpSetTime);
                    break;

                case TODPowerUpSetMode.SpecificDate:
                    //
                    // Set TOD clock to the specified date, with the current time.
                    //
                    _todValue = GetXeroxTime(PowerUpSetTime.Add(DateTime.Now.TimeOfDay));
                    break;

                case TODPowerUpSetMode.NoChange:
                    //
                    // Do nothing.
                    //
                    break;
            }
            _lock.ExitWriteLock();
        }

        private uint GetXeroxTime(DateTime time)
        {
            //
            // The Star's epoch is 1/1/1901.
            //
            long dateTicks = time.Ticks;
            long epochTicks = new DateTime(1901, 1, 1, 0, 0, 0).ToLocalTime().Ticks;

            long adjustedTicks = dateTicks - epochTicks;

            // Ticks are 100nS.
            uint seconds = (uint)(adjustedTicks / 10000000);

            return seconds;
        }

        private void TimerTick(object context)
        {
            //
            // One real second has elapsed.
            // We raise the interrupt flag and increment
            // the clock value.
            //
            _lock.EnterWriteLock();
                _interrupt = true;
                _todValue++;
            _lock.ExitWriteLock();
        }

        private bool _interrupt;
        private bool _powerLoss;
        private UInt32 _todValue;
        private int _todReadBit;

        private TODAccessMode _mode;

        // Timer for real-time clocking and a lock to make things
        // thread-safe.  The lock is probably overkill but let's do this right.
        private Timer _timer;
        private ReaderWriterLockSlim _lock;
    }
}
