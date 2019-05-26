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


using D.Logging;
using System;

namespace D.IOP
{
    public enum AltBootValues
    {
        None = -1,
        DiagnosticRigid = 0,
        Rigid,
        Floppy,
        Ethernet,
        DiagnosticEthernet,
        DiagnosticFloppy,
        AlternateEthernet,
        DiagnosticTrident1,
        DiagnosticTrident2,
        DiagnosticTrident3,
        HeadCleaning
    }

    public class MiscIO : IIOPDevice
    {
        public MiscIO(IOProcessor iop)
        {
            //
            // Keep a reference to the IOP we belong to
            // so we can poll the interrupt status of various
            // devices.
            //
            _iop = iop;

            _todClock = new TODClock();

            // Default to no alt boot
            _altBoot = AltBootValues.None;

            Reset();
        }        

        public int[] ReadPorts
        {
            get { return _readPorts; }
        }

        public int[] WritePorts
        {
            get { return _writePorts; }
        }

        /// <summary>
        /// Allows UI to be alerted when the MP value changes.
        /// </summary>
        public delegate void MPChangedEventHandler();

        public MPChangedEventHandler MPChanged;


        public AltBootValues AltBoot
        {
            get
            {
                return _altBoot;
            }

            set
            {
                _altBoot = value;
                _altBootCounter = (int)value;
            }
        }

        public void Reset()
        {
            _mPanelValue = 0;
            _mPanelBlank = true;

            _lastClockFlags = 0;
            _dmaTestValue = 0;
            
            _altBootCounter = (int)_altBoot;

            _todClock.Reset();
        }

        public void WritePort(int port, byte value)
        {
            switch (port)
            {
                case 0x8d:
                    // i8253 Timer channel #1 - used to set the Keyboard bell (tone) frequency.
                    // This is a 16-bit value loaded one byte at a time, LSB first.
                    // Send the word off to the tone generator.
                    _iop.Beeper.LoadPeriod(value);
                    break;

                case 0x8f:
                    // i8253 Timer Mode.
                    // This is used to control the timer used for the Keyboard bell
                    // and for the USART.  It specifies which timer will be active,
                    // how that timer's interval is loaded, and what the output waveform
                    // looks like.
                    // At this time there's no particular reason to pay attention to what
                    // gets written here, as we don't actually emulate the i8253.
                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port Timer Mode written {0:x2}", value);
                    break;

                case 0xd0:
                    //
                    // DMA Test Register
                    // At this point, it appears all this does is store the value written,
                    // and return it when read.
                    //
                    _dmaTestValue = value;
                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port DMATest write {0:x2}", value);
                    break;

                case 0xe9:      // KB, MP, TOD clocks
                    DoMiscClock(value);
                    break;

                case 0xea:      // Clear TOD interrupt
                    _todClock.ClearInterrupt();
                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO TOD interrupt clear.");
                    break;

                case 0xed:      // Clear Mouse X,Y counters
                    _iop.Mouse.Clear();
                    break;

                case 0xef:      // KB, MP, TOD control
                    //
                    // Control bits:
                    // 0x40 - pReadKBData - read KB data
                    // 0x20 - KBTone - KB speaker bit
                    // 0x10 - KBDiag - Set KB Diag mode
                    // 0x08 - BlankMPanel - Blank MPanel bit
                    // 0x04 - ReadTimeMode - Read TOD mode bit
                    // 0x02 - ClearTimeMode - Clear TOD mode bit
                    // 0x01 - SetTimeMode - Set TOD mode bit
                    //
                    _mPanelBlank = (value & 0x08) != 0;
                    MPChanged();

                    if ((value & 0x40) != 0)
                    {
                        // Prime the next byte of keyboard data.
                        _iop.Keyboard.NextData();
                        if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO Keyboard data clock.");
                    }

                    if ((value & 0x20) != 0)
                    {
                        _iop.Beeper.EnableTone();
                    }
                    else
                    {
                        _iop.Beeper.DisableTone();
                    }

                    if ((value & 0x10) != 0)
                    {
                        _iop.Keyboard.EnableDiagnosticMode();
                        if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO Keyboard diagnostic mode entered.");
                    }

                    if ((value & 0x04) != 0)
                    {
                        _todClock.SetMode(TODAccessMode.Read);
                    }

                    if ((value & 0x02) != 0)
                    {
                        _todClock.SetMode(TODAccessMode.Clear);
                    }

                    if ((value & 0x01) != 0)
                    {
                        _todClock.SetMode(TODAccessMode.Set);
                    }

                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unexpected write to port {0:x2}", port));
            }
        }

        public byte ReadPort(int port)
        {
            byte value;
            switch(port)
            {
                case 0xd0:
                    //
                    // DMA Test Register:
                    // Just return whatever value was written.
                    //
                    value = _dmaTestValue;
                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port DMATest read {0:x2}", value);
                    break;

                case 0xef:
                    //
                    // MiscInput1: AltBoot,TimeData,PowerFailed,TODInt,CSParError,MouseSw1,Sw2,Sw3
                    //
                    // Provide the AltBoot switch so we can allow floppy booting:
                    // The way the boot prom selects a boot device is to:
                    // 1) check for the AltBoot bit (meaning the alternate boot switch is held down)
                    // 2) if so, increment MP, wait 1 second.
                    // 3) repeat 1 + 2 until AltBoot is no longer set.
                    // 4) the final value is the device to be booted from.
                    //
                    // We decrement our boot device counter on every read until we hit zero at which point
                    // we "release" the button.
                    //
                    if (_altBootCounter > 0)
                    {
                        value = (int)MiscInput1Flags.AltBoot;
                        _altBootCounter--;
                    }
                    else
                    {
                        value = 0;
                    }

                    //
                    // OR in other bits
                    //
                    value = (byte)(value |
                        _todClock.ReadClockBit() |
                        (_todClock.PowerLoss ? 0x20 : 0x0) |
                        (_todClock.Interrupt ? 0x10 : 0x0) |
                        (int)(_iop.Mouse.Buttons) |
                        (int)MiscInput1Flags.CSParity /* active low, we don't want parity errors */);

                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port MiscInput1 read {0:x2}", value);
                    break;

                case 0xe9:
                    //
                    // Interrupt status register.
                    // Most of these aren't real interrupts (they don't trigger an 8085 interrupt) but are simply
                    // status flags raised by various bits of hardware that get polled by the main IOP code loop.
                    // This register is not a latch, it merely buffers these signals which are generated by their 
                    // respective devices.
                    // We combine those bits here from their various sources.  All of these signals are active low.
                    //
                    // Add more as more things get implemented...
                    // 
                    value = (byte)~(
                        (_iop.FloppyController.Interrupt ? 0x80 : 0x00) |
                        (_iop.Keyboard.DataReady() ? 0x40 : 0x00)); 
                        /* TODO: disabled until it can be completed
                        (_iop.Printer.TxRequest ? 0x20 : 0x00) |
                        (_iop.Printer.RxRequest ? 0x10 : 0x00)); */

                    // if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port Interrupt Status read {0:x2}", value);
                    break;

                case 0xea:
                    //
                    // Keyboard data latch.  Data is inverted, and bit 0 (msb) indicates keystroke up or down (1 = down).
                    //                    
                    value = (byte)(~_iop.Keyboard.ReadData());
                    if (Log.Enabled) Log.Write(LogComponent.IOPMisc, "Misc IO port Keyboard Data read {0:x2}", value);
                    break;

                case 0xed:
                    //
                    // Mouse X counter
                    //
                    value = (byte)_iop.Mouse.MouseX;
                    break;

                case 0xee:
                    //
                    // Mouse Y counter
                    //
                    value = (byte)_iop.Mouse.MouseY;
                    break;
                           
                default:
                    value = 0;                    
                    break;
            }            

            return value;
        }

        /// <summary>
        /// The value of the MP display (displayed in red LEDs on the front of the Star)
        /// </summary>
        public int MPanelValue
        {
            get { return _mPanelValue; }
        }

        /// <summary>
        /// Whether the MP display is currently turned on
        /// </summary>
        public bool MPanelBlank
        {
            get { return _mPanelBlank; }
        }

        public TODClock TODClock
        {
            get { return _todClock; }
        }

        private void DoMiscClock(byte clockFlags)
        {
            //
            // From code in BootSubs.asm (not coincidentally labeled DoMiscClock):
            // The various clocks are clocked manually by the 8085 by writing a zero
            // to the appropriate clock bit followed by a 1 to the clock bit, on a 0->1 transition
            // data is clocked into the appropriate register, or cleared/incremented for the MPanel display.
            //

            //
            // on a 1->0 transition for a clock bit we will take the appropriate action.
            //
            for (int clockFlag = 0x1; clockFlag < 0x100; clockFlag = clockFlag << 1)
            {                
                if ((clockFlags & clockFlag) == 0 && 
                    (_lastClockFlags & clockFlag) != 0)
                {
                    switch((ClockFlags)clockFlag)
                    {
                        case ClockFlags.ClrMPanel:
                            _mPanelValue = 0;
                            MPChanged();
                            break;

                        case ClockFlags.IncMPanel:
                            _mPanelValue = (_mPanelValue + 1) % 10000;
                            MPChanged();
                            break;

                        case ClockFlags.TODRead:
                            _todClock.ClockBit(TODClockType.Read);
                            break;

                        case ClockFlags.TODSetA:
                            _todClock.ClockBit(TODClockType.SetA);
                            break;

                        case ClockFlags.TODSetB:
                            _todClock.ClockBit(TODClockType.SetB);
                            break;

                        case ClockFlags.TODSetC:
                            _todClock.ClockBit(TODClockType.SetC);
                            break;

                        case ClockFlags.TODSetD:
                            _todClock.ClockBit(TODClockType.SetD);
                            break;
                    }
                }
            }

            _lastClockFlags = clockFlags;
        }

        // MP data
        private bool _mPanelBlank;
        private int _mPanelValue;

        // Alt boot counter -- decremented on access to MiscInput1, used to simulate
        // holding down of AltBoot button for N seconds to select boot device.
        private int _altBootCounter;

        // The value to set the AltBoot counter to at reset.
        private AltBootValues _altBoot;

        // Clock register data
        private int _lastClockFlags;

        // DMA Test Register data
        private byte _dmaTestValue;

        // TOD Clock
        private TODClock _todClock;

        // Reference to the IOP we belong to.
        private IOProcessor _iop;

        private readonly int[] _readPorts = new int[] 
            {
                0xd0,       // DMA Test Register
                0xe9,       // Interrupt request bits (read)
                0xea,       // Keyboard data latch
                0xed,       // Mouse X counter
                0xee,       // Mouse Y counter
                0xef        // Miscellaneous input
            };

        private readonly int[] _writePorts = new int[] 
            {
                0x8d,       // i8253 Timer Control 1 (Tone frequency)
                0x8f,       // i8253 Timer Mode
                0xd0,       // DMA Test Register
                0xe9,       // KB, MP, TOD clocks (write)
                0xea,       // Clear TOD interrupt (write)
                0xed,       // Clear Mouse X,Y counters
                0xef        // KB, MP, TOD control (write)
            };

        //
        // Misc clocks flags
        //
        [Flags]
        private enum ClockFlags
        {
            ClrMPanel = 0x40,
            IncMPanel = 0x20,
            TODRead =   0x10,
            TODSetA =   0x08,
            TODSetB =   0x04,
            TODSetC =   0x02,
            TODSetD =   0x01,
        }

        [Flags]
        private enum MiscInput1Flags
        {
            AltBoot = 0x80,
            TODData = 0x40,
            PowerFailed = 0x20,
            TODInt = 0x10,
            CSParity = 0x08,
            MouseSw3 = 0x4,
            MouseSw2 = 0x2,
            MouseSw1 = 0x1,
        }

        [Flags]
        private enum InterruptRequestFlags
        {
            Floppy = 0x80,
            Keyboard = 0x40,
            PrinterTx = 0x20,
            PrinterRx = 0x10,
            Misc = 0x08,
            RS232 = 0x04,
            LSEPTx = 0x02,
            LSEPRx = 0x01,
        }

    }
}
