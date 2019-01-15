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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace D
{
    /// <summary>
    /// HighResTimer gives us access to NT's very-high-resolution PerformanceCounters.
    /// This gives us the precision we need to sync emulation to any speed we desire.
    /// </summary>
    public sealed class HighResTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        public HighResTimer()
        {
            // What's the frequency, Kenneth?
            if (QueryPerformanceFrequency(out _frequency) == false)
            {
                // high-performance counter not supported
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Returns the current time in seconds.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentTime()
        {
            long currentTime;
            QueryPerformanceCounter(out currentTime);

            return (double)(currentTime) / (double)_frequency;
        }

        private long _frequency;
    }

    public sealed class FrameTimer
    {
        [DllImport("winmm.dll", EntryPoint = "timeGetDevCaps", SetLastError = true)]
        static extern UInt32 TimeGetDevCaps(ref TimeCaps timeCaps, UInt32 sizeTimeCaps);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        static extern UInt32 TimeBeginPeriod(UInt32 uPeriod);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("kernel32.dll", EntryPoint = "CreateTimerQueue")]
        public static extern IntPtr CreateTimerQueue();

        [DllImport("kernel32.dll", EntryPoint = "DeleteTimerQueueEx")]
        public static extern bool DeleteTimerQueue(IntPtr hTimerQueue, IntPtr hCompletionEvent);

        [DllImport("kernel32.dll", EntryPoint = "CreateTimerQueueTimer")]
        public static extern bool CreateTimerQueueTimer(out IntPtr phNewTimer, IntPtr hTimerQueue, IntPtr Callback, IntPtr Parameter, UInt32 DueTime, UInt32 Period, uint Flags);

        [DllImport("kernel32.dll", EntryPoint = "ChangeTimerQueueTimer")]
        public static extern bool ChangeTimerQueueTimer(IntPtr hTimerQueue, IntPtr hTimer, UInt32 DueTime, UInt32 Period);


        [DllImport("kernel32.dll", EntryPoint = "DeleteTimerQueueTimer")]
        public static extern bool DeleteTimerQueueTimer(IntPtr hTimerQueue, IntPtr hTimer, IntPtr hCompletionEvent);

        [StructLayout(LayoutKind.Sequential)]
        public struct TimeCaps
        {
            public UInt32 wPeriodMin;
            public UInt32 wPeriodMax;
        };

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void UnmanagedTimerCallback(IntPtr param, bool timerOrWait);

        /// <summary>
        /// FrameTimer provides a simple method to synchronize execution to a given framerate.
        /// Calling WaitForFrame() blocks until the start of the next frame.
        /// 
        /// NOTE: This code uses the Win32 TimerQueue APIs instead of the System.Threading.Timer
        /// APIs because the .NET APIs do not allow execution of the callback on the timer's thread --
        /// it queues up a new worker thread.  This lowers the accuracy of the timer, and since we
        /// need all the precision we can get they're not suitable here.
        /// </summary>
        /// <param name="framesPerSecond">The frame rate to sync to.</param>
        public FrameTimer(double framesPerSecond)
        {
            //
            // Set the timer to the minimum value (1ms).  This should be supported on any modern x86 system.
            // If not, too bad...
            //
            UInt32 res = TimeBeginPeriod(1);

            if (res != 0)
            {
                throw new InvalidOperationException("Unable to set timer period.");
            }

            //
            // Create a new timer queue
            // 
            _hTimerQueue = CreateTimerQueue();

            if (_hTimerQueue == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to create timer queue.");
            }

            //
            // Since we only have a resolution of 1ms, we have to do some hackery to get a slightly more accurate framerate.
            // (60 fields/sec requires 16 2/3s ms frame delay.)
            // We alternate between two rates at varying intervals and this gets us fairly close to the desired frame rate.            
            //
            _callback = new UnmanagedTimerCallback(TimerCallbackFn);

            _highPeriod = (uint)Math.Ceiling(1000.0 * (1.0 / framesPerSecond));
            _lowPeriod = (uint)Math.Floor(1000.0 * (1.0 / framesPerSecond));
            _periodTenths = _periodSwitch = (uint)((1000.0 * (1.0 / framesPerSecond) - Math.Floor(1000.0 * (1.0 / framesPerSecond))) * 10.0);

            if (!CreateTimerQueueTimer(out _hTimer, _hTimerQueue, Marshal.GetFunctionPointerForDelegate(_callback), IntPtr.Zero, _lowPeriod, _lowPeriod, 0x00000020 /* execute in timer thread */))
            {
                throw new InvalidOperationException("Unable to create timer queue timer.");
            }

            _event = new AutoResetEvent(false);

            _lowTimer = 0;
        }

        ~FrameTimer()
        {
            //
            // Clean stuff up
            //
            try
            {
                DeleteTimerQueueTimer(_hTimerQueue, _hTimer, IntPtr.Zero);
                DeleteTimerQueue(_hTimerQueue, IntPtr.Zero);
            }
            catch
            {
                // Eat exceptions (for Mono)
            }

            //
            // Fire off a final event to release any call that's waiting...
            //
            if (_event != null)
            {
                _event.Set();
            }
        }

        /// <summary>
        /// Waits for the timer to fire.
        /// </summary>
        public void WaitForFrame()
        {
            _event.WaitOne();
        }

        /// <summary>
        /// Callback from timer queue.  Work done here is executed on the timer's thread, so must be quick.
        /// </summary>
        /// <param name="lpParameter"></param>
        /// <param name="TimerOrWaitFired"></param>         
        private void TimerCallbackFn(IntPtr lpParameter, bool TimerOrWaitFired)
        {
            _event.Set();
            _lowTimer++;

            if (_lowTimer >= _periodSwitch)
            {
                _lowTimer = 0;
                _period = !_period;
                ChangeTimerQueueTimer(_hTimerQueue, _hTimer, _period ? _lowPeriod : _highPeriod, _period ? _lowPeriod : _highPeriod);

                _periodSwitch = !_period ? _periodTenths : 10 - _periodTenths;
            }
        }

        private IntPtr _hTimerQueue;
        private IntPtr _hTimer;
        private AutoResetEvent _event;
        private UnmanagedTimerCallback _callback;
        private uint _lowPeriod;
        private uint _highPeriod;
        private uint _periodSwitch;
        private uint _periodTenths;
        private int _lowTimer;
        private bool _period;
    }
}