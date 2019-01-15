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


using D.IOP;
using D.Logging;
using System;

namespace D.CP
{
    // 
    // From SysDefs.asm:
    // Bits 0:5 - (IOPWait', SwTAddr', IOPattn, CPDmaMode, CPDmaIn)
    // (in the usual Xerox reverse order)
    //
    [Flags]
    public enum CPControlFlags

    {
        IOPWait_ = 0x80,
        SwTAddr_ = 0x40,
        IOPattn = 0x20,
        CPDmaMode = 0x10,
        CPDmaIn = 0x08,
    }

    //
    // From IOP schematics, p 15-17:
    //
    public enum CPStatusFlags
    {
        CPAttn = 0x80,
        EmuWake = 0x40,
        IOPAttn_ = 0x20,
        CPDmaMode_ = 0x10,
        CPDmaIn_ = 0x08,
        CPInIntReq_ = 0x04,
        CPOutIntReq_ = 0x2,
        CPDmaComplete_ = 0x1,
    }

    [Flags]
    public enum IOPCtlFlags
    {
        EmuWake = 0x8,
        CPAttn = 0x4,
        WakeMode0 = 0x2,
        WakeMode1 = 0x1,
    }

    [Flags]
    public enum IOPStatusFlags
    {
        IOPAttn = 0x20,
        EmuWake_ = 0x10,
        CPAttn_ = 0x8,
        WakeMode0_ = 0x4,
        WakeMode1_ = 0x2,
        IOPReq = 0x1,
    }

    /// <summary>
    /// The Dandelion Central Processor, I/O implmentation.
    /// This partial class implements the CP<->IOP communication channel,
    /// and IOP microcode loading logic.
    /// </summary>
    public partial class CentralProcessor : IIOPDevice, IDMAInterface
    {
        //
        // IOP port info
        //
        public int[] ReadPorts
        {
            get { return _readPorts; }
        }

        public int[] WritePorts
        {
            get { return _writePorts; }
        }

        public void WritePort(int port, byte value)
        {
            switch ((PortWriteRegister)port)
            {
                case PortWriteRegister.CPDataOut:
                    WriteCPInBuffer(value);
                    break;

                case PortWriteRegister.CPControl:
                    WriteCPCtl(value);
                    break;

                case PortWriteRegister.CPClrDmaComplete:
                    _cpDmaComplete_ = false;
                    if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "CP DMA complete flag cleared.");
                    break;

                case PortWriteRegister.CPCSa:
                case PortWriteRegister.CPCSb:
                case PortWriteRegister.CPCSc:
                case PortWriteRegister.CPCSd:
                case PortWriteRegister.CPCSe:
                case PortWriteRegister.CPCSf: // CS microcode word, MSB ($F8) to LSB ($FD)
                    WriteIOPMicrocodeWord(port - 0xf8, (byte)~value);

                    if (port == 0xfd)
                    {
                        if (Log.Enabled) Log.Write(LogComponent.CPMicrocodeLoad, "CS word {0:x3} completed: {1:x12} {2}", _tpc[6], _microcode[_tpc[6]], new Microinstruction(_microcode[_tpc[6]]).Disassemble(-1));
                    }
                    break;

                case PortWriteRegister.TPCHigh:   // TPC high : TPCAddr[0:2],,TPCData[0:4]'
                    _tpcAddr = value >> 5;
                    _tpcTemp = ((~value & 0x1f) << 7);                    
                    if (Log.Enabled) Log.Write(LogComponent.CPTPCLoad, "TPC high written: TPC[{0}] ({1}) is now {2:x3}", _tpcAddr, (TaskType)_tpcAddr, _tpcTemp);
                    break;

                case PortWriteRegister.TPCLow:    // TPC low : don't care,,TPCData[5:11]'
                    _tpc[_tpcAddr] = _tpcTemp | (~value & 0x7f);
                    if (Log.Enabled) Log.Write(LogComponent.CPTPCLoad, "TPC low written: TPC[{0}] ({1}) is now {2:x3}", _tpcAddr, (TaskType)_tpcAddr, _tpc[_tpcAddr]);
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unexpected write to port {0:x2}", port));
            }
        }

        public byte ReadPort(int port)
        {
            byte value = 0;
            switch ((PortReadRegister)port)
            {
                case PortReadRegister.CPDataIn:
                    value = ReadCPOutBuffer();
                    break;

                case PortReadRegister.CPStatus:
                    value = ReadCPStatus();
                    break;

                case PortReadRegister.CPCS0:
                case PortReadRegister.CPCS1:
                case PortReadRegister.CPCS2:
                case PortReadRegister.CPCS3:
                case PortReadRegister.CPCS4:
                case PortReadRegister.CPCS5: // CS microcode word, MSB ($F8) to LSB ($FD)
                    value = ReadIOPMicrocodeWord(port - 0xf8);
                    break;

                case PortReadRegister.CPCS6: // TPC high : TC[0:3],,TPCData[0:3]'
                    value = (byte)~((~_tc[_tpcAddr] << 4) | ((_tpc[_tpcAddr] & 0xf00) >> 8));
                    break;

                case PortReadRegister.CPCS7: // TPC low : TPCData[4:11]'
                    value = (byte)(~_tpc[_tpcAddr]);
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unexpected read from port {0:x2}", port));
            }

            if (Log.Enabled) Log.Write(LogComponent.CPControl, "CP port {0:x2}({1}) read {2:x2}", port, (PortReadRegister)port, value);

            return value;
        }

        //
        // IDMAInterface implementation
        //
        /// <summary>
        /// DMA Request: device request to obtain a DMA cycle from the DMA controller
        /// </summary>
        public bool DRQ
        {
            get
            {
                if (_cpDmaMode)     // DMA is enabled
                {
                    if (_cpDmaIn)
                    {
                        return _outLatched;
                    }
                    else
                    {
                        return !_inLatched;                        
                    }
                }
                else
                {
                    return false;
                }
            }       
        }

        /// <summary>
        /// Writes a single byte to the device from the DMA controller
        /// </summary>
        /// <param name="value"></param>
        public void DMAWrite(byte value)
        {
            WriteCPInBuffer(value);
        }

        /// <summary>
        /// Reads a single byte from the device to the DMA controller
        /// </summary>
        /// <returns></returns>
        public byte DMARead()
        {
            return ReadCPOutBuffer();
        }

        public void DMAComplete()
        {
            _cpDmaComplete_ = true;
            if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "CP DMA complete, flag set.");
        }

        private byte ReadCPOutBuffer()
        {
            if (!_outLatched)
            {
                if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.CPControl, "CP data out not latched on IOP read.");
            }

            //
            // Clear the output data latched flag.
            //
            _outLatched = false;

            //
            // Clear the CP->IOP interrupt flag (active low): the IOP has read the available data.
            //
            _cpInIntReq_ = true;

            UpdateIOPTaskWakeup();

            //
            // Return the buffer data
            //
            return _cpInData;
        }

        private void WriteCPInBuffer(byte value)
        {
            if (Log.Enabled) Log.Write(LogComponent.CPControl, "CP data out write ({0:x2})", value);

            if (_inLatched)
            {
                if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.CPControl, "CP data out already latched on IOP write", value);
            }

            //
            // Clear the IOP->CP interrupt (active low): The CP has yet to read the data provided by the IOP.
            //
            _cpOutIntReq_ = true;
            _cpOutData = value;

            //
            // Let the CP know there's data available.
            //
            _inLatched = true;

            UpdateIOPTaskWakeup();
        }

        /// <summary>
        /// Writes one byte of the microcode word currently pointed to by
        /// TPC[6].
        /// </summary>
        /// <param name="b"></param>
        /// <param name="value"></param>
        private void WriteIOPMicrocodeWord(int b, byte value)
        {
            //
            // In true Xerox fashion, the 48 bits of the microcode word provided by the IOP
            // are not in order from MSB to LSB or anything simple like that.  Though most of them are.
            // From SysDefs.asm:
            //
            // "; Write (all CSi are complemented values):
            //  CSa equ CSBase + 0; CS Byte a: rA[0:3],,rB[0:3]
            //  CSb equ CSBase + 1; CS Byte b: aS[0:2],,aF[0:2],,aD[0:1]
            //  CSc equ CSBase + 2; CS Byte c: EP,,CIN,,EnSU,,mem,,fS[0:3]
            //  CSd equ CSBase + 3; CS Byte d: fY[0:3], INIA[0:3]
            //  CSe equ CSBase + 4; CS Byte e: fX[0:3], INIA[4:7]
            //  CSf equ CSBase + 5; CS Byte f: fZ[0:3], INIA[8:11]"
            //
            // "value" is expected to already be complemented on call to WriteIOPMicrocodeWord.
            //


            // TPC register 6 is always used for IOP microcode writes.
            ulong word = _microcode[_tpc[6]];
            switch (b)
            {
                case 0:
                    word = (word & 0x00ffffffffff) | ((ulong)value << 40);
                    break;

                case 1:
                    word = (word & 0xff00ffffffff) | ((ulong)value << 32);
                    break;

                case 2:
                    word = (word & 0xffff00ffffff) | ((ulong)value << 24);
                    break;

                case 3:
                    {
                        // FY[0:3], INIA[0:3]
                        ulong fy = ((ulong)value & 0xf0) >> 4;
                        ulong inia = ((ulong)value & 0xf);
                        word = (word & 0xfffffff0f0ff) | (fy << 16) | (inia << 8);
                    }
                    break;

                case 4:
                    {
                        // FX[0:3], INIA[4:7]
                        ulong fx = ((ulong)value & 0xf0) >> 4;
                        ulong inia = ((ulong)value & 0xf);
                        word = (word & 0xffffff0fff0f) | (fx << 20) | (inia << 4);
                    }
                    break;

                case 5:
                    {
                        // FZ[0:3], INIA[8:11]
                        ulong fz = ((ulong)value & 0xf0) >> 4;
                        ulong inia = ((ulong)value & 0xf);
                        word = (word & 0xffffffff0ff0) | (fz << 12) | inia;
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid byte number for microcode word.");
            }

            _microcode[_tpc[6]] = word;
            _microcodeCache[_tpc[6]] = new Microinstruction(word);
        }

        /// <summary>
        /// Reads one byte of the microcode word currently pointed to by
        /// TPC[6].
        /// </summary>
        /// <param name="b"></param>
        /// <param name="value"></param>
        private byte ReadIOPMicrocodeWord(int b)
        {
            byte value = 0;

            // TPC register 6 is always used for IOP microcode reads.
            ulong word = _microcode[_tpc[6]];
            switch (b)
            {
                case 0:
                    value = (byte)(_microcode[_tpc[6]] >> 40);
                    break;

                case 1:
                    value = (byte)(_microcode[_tpc[6]] >> 32);
                    break;

                case 2:
                    value = (byte)(_microcode[_tpc[6]] >> 24);
                    break;

                case 3:
                    {
                        // FY[0:3], INIA[0:3]
                        value = (byte)(((_microcode[_tpc[6]] >> 12) & 0xf0) | ((_microcode[_tpc[6]] >> 8) & 0xf));
                    }
                    break;

                case 4:
                    {
                        // FX[0:3], INIA[4:7]
                        value = (byte)(((_microcode[_tpc[6]] >> 16) & 0xf0) | ((_microcode[_tpc[6]] >> 4) & 0xf));
                    }
                    break;

                case 5:
                    {
                        // FZ[0:3], INIA[8:11]
                        value = (byte)(((_microcode[_tpc[6]] >> 8) & 0xf0) | (_microcode[_tpc[6]] & 0xf));
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid byte number for microcode word.");
            }

            return value;
        }

        private void WriteCPCtl(byte value)
        {
            if (Log.Enabled) Log.Write(LogComponent.CPControl, "CP control write {0} ({1:x2})", (CPControlFlags)value, value);

            bool oldIopWait = _iopWait_;
            _iopWait_ = (value & (int)CPControlFlags.IOPWait_) == 0; // inverted sense
            _swTAddr = (value & (int)CPControlFlags.SwTAddr_) == 0; // ditto
            _iopAttn = (value & (int)CPControlFlags.IOPattn) != 0;
            _cpDmaMode = (value & (int)CPControlFlags.CPDmaMode) != 0;
            _cpDmaIn = (value & (int)CPControlFlags.CPDmaIn) != 0;

            if (oldIopWait != _iopWait_)
            {
                //
                // Raising IOPWait causes a Kernel wakeup to be requested so that the Kernel task
                // will run when the CP is allowed to run again.
                //
                for(int i=0;i<7;i++)
                {
                    SleepTask((TaskType)i);
                }

                WakeTask(TaskType.Kernel);
                _currentTask = TaskType.Kernel;

                //
                // Reset any error status
                //
                _emulatorErrorTrap = false;
                _emulatorErrorTrapClickCount = 0;
            }
        }

        private byte ReadCPStatus()
        {
            return (byte)
                ((_cpDmaComplete_ ? CPStatusFlags.CPDmaComplete_ : 0) |
                 (!_cpOutIntReq_ ? CPStatusFlags.CPOutIntReq_ : 0) |
                 (!_cpInIntReq_ ? CPStatusFlags.CPInIntReq_ : 0) |
                 (!_cpDmaIn ? CPStatusFlags.CPDmaIn_ : 0) |
                 (!_cpDmaMode ? CPStatusFlags.CPDmaMode_ : 0) |
                 (_emuWake ? CPStatusFlags.EmuWake : 0) |
                 (!_cpAttn ? CPStatusFlags.CPAttn : 0));
        }

        private void WriteIOPCtl(byte value)
        {
            if (Log.Enabled) Log.Write(LogComponent.CPControl, "IOPCtl<- {0} ({1:x2})", (IOPCtlFlags)value, value);

            _wakeMode1 = (value & (int)IOPCtlFlags.WakeMode1) != 0;
            _wakeMode0 = (value & (int)IOPCtlFlags.WakeMode0) != 0;
            _cpAttn = (value & (int)IOPCtlFlags.CPAttn) != 0;
            _emuWake = (value & (int)IOPCtlFlags.EmuWake) != 0;           

            _wakeMode = (IOPTaskWakeMode)((_wakeMode0 ? 0x2 : 0x0) | (_wakeMode1 ? 0x1 : 0x0));
            if (Log.Enabled) Log.Write(LogComponent.CPControl, "IOP Wake mode is {0}", _wakeMode);

            // See if the wake status of the IOP task needs to change.
            UpdateIOPTaskWakeup();
        }

        private byte ReadIOPStatus()
        {
            return (byte)
                 ((_iopReq ? IOPStatusFlags.IOPReq : 0) |
                  (!_wakeMode1 ? IOPStatusFlags.WakeMode1_ : 0) |
                  (!_wakeMode0 ? IOPStatusFlags.WakeMode0_ : 0) |
                  (!_cpAttn ? IOPStatusFlags.CPAttn_ : 0) |
                  (!_emuWake ? IOPStatusFlags.EmuWake_ : 0) |
                  (_iopAttn ? IOPStatusFlags.IOPAttn : 0));
        }

        private byte ReadIOPData()
        {
            if (!_inLatched)
            {
                if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.CPControl, "CP data not latched on <-IOPData.");
            }

            //
            // Clear the IOP request flag
            //
            _inLatched = false;

            //
            // Raise the IOP->CP flag (active low): the CP has read the available data.
            //
            _cpOutIntReq_ = false;

            UpdateIOPTaskWakeup();

            return _cpOutData;
        }

        private void WriteIOPData(byte value)
        {
            if (_outLatched)
            {
                if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.CPControl, "CP data already latched on IOPOData<-");
            }

            //
            // Latch the output data
            //
            _outLatched = true;

            //
            // Raise the CP->IOP interrupt flag (active low): there is data available for the IOP to read.
            //
            _cpInIntReq_ = false;            

            //
            // Fill the CP->IOP buffer
            //
            _cpInData = value;

            UpdateIOPTaskWakeup();
        }

        private void UpdateIOPTaskWakeup()
        {
            //
            // Wake or sleep the IOP task as appropriate based on the current wake mode
            // and the status of the I/O channel.
            //
            switch (_wakeMode)
            {
                case IOPTaskWakeMode.Always:
                    //
                    // Like the label says, we wake up the IOP task unconditionally.
                    //
                    _iopReq = true;
                    WakeTask(TaskType.IOP);
                    break;

                case IOPTaskWakeMode.Input:
                    //
                    // If there's input waiting, wake the IOP task.
                    //
                    if (_inLatched)
                    {
                        _iopReq = true;
                        WakeTask(TaskType.IOP);
                    }
                    else
                    {
                        _iopReq = false;
                        SleepTask(TaskType.IOP);
                    }
                    break;

                case IOPTaskWakeMode.Output:
                    //
                    // If the output buffer is currently empty, wake the IOP task.
                    //
                    if (!_outLatched)
                    {
                        _iopReq = true;
                        WakeTask(TaskType.IOP);
                    }
                    else
                    {
                        _iopReq = false;
                        SleepTask(TaskType.IOP);
                    }
                    break;

                case IOPTaskWakeMode.Disabled:
                    _iopReq = false;
                    SleepTask(TaskType.IOP);
                    break;
            }
        }

        private enum PortReadRegister
        {
            CPDataIn = 0xeb,
            CPStatus = 0xec,
            CPCS0 = 0xf8,
            CPCS1 = 0xf9,
            CPCS2 = 0xfa,
            CPCS3 = 0xfb,
            CPCS4 = 0xfc,
            CPCS5 = 0xfd,
            CPCS6 = 0xfe,
            CPCS7 = 0xff,
        }

        private enum PortWriteRegister
        {
            CPDataOut = 0xeb,
            CPControl = 0xec,
            CPClrDmaComplete = 0xee,
            CPCSa = 0xf8,
            CPCSb = 0xf9,
            CPCSc = 0xfa,
            CPCSd = 0xfb,
            CPCSe = 0xfc,
            CPCSf = 0xfd,
            TPCHigh = 0xfe,
            TPCLow = 0xff,
        }


        // From the IOP schematic:
        //  - 00 = Disabled(no wakeups)
        //  - 01 = Input(wakeup when Input from IOP is available)
        //  - 10 = Output(wakeup when IOP is ready for data from CP)
        //  - 11 = Always wake up
        private enum IOPTaskWakeMode
        {
            Disabled = 0,
            Input,
            Output,
            Always,
        }

        //
        // IOP port data
        //
        private readonly int[] _readPorts = new int[]
        {
                (int)PortReadRegister.CPDataIn,     // CP data in
                (int)PortReadRegister.CPStatus,     // CP port status
                (int)PortReadRegister.CPCS0,       // control store word, MSB
                (int)PortReadRegister.CPCS1,
                (int)PortReadRegister.CPCS2,
                (int)PortReadRegister.CPCS3,
                (int)PortReadRegister.CPCS4,
                (int)PortReadRegister.CPCS5,       // control store word, LSB
                (int)PortReadRegister.CPCS6,       // TPC high
                (int)PortReadRegister.CPCS7,       //     low
        };

        private readonly int[] _writePorts = new int[]
        {
                (int)PortWriteRegister.CPDataOut,
                (int)PortWriteRegister.CPControl,
                (int)PortWriteRegister.CPClrDmaComplete,
                (int)PortWriteRegister.CPCSa,       // control store word, MSB
                (int)PortWriteRegister.CPCSb,
                (int)PortWriteRegister.CPCSc,
                (int)PortWriteRegister.CPCSd,
                (int)PortWriteRegister.CPCSe,
                (int)PortWriteRegister.CPCSf,       // control store word, LSB
                (int)PortWriteRegister.TPCHigh,
                (int)PortWriteRegister.TPCLow,
        };

        //
        // Control data, IOP
        //
        private bool _cpDmaComplete_;
        private bool _iopWait_;      // Waiting for IOP to wake us
        private bool _swTAddr;
        private bool _iopAttn;
        private bool _cpDmaMode;
        private bool _cpDmaIn;

        //
        // Control data, CP
        //
        private bool _wakeMode1;
        private bool _wakeMode0;
        private bool _cpAttn;
        private bool _emuWake;
        private IOPTaskWakeMode _wakeMode;

        //
        // Status data
        //        
        private bool _cpOutIntReq_;
        private bool _cpInIntReq_;
        private bool _outLatched;       // Data from CP->IOP latched
        private bool _inLatched;        // Data from IOP->CP latched
        private bool _iopReq;

        //
        // CP<->IOP data buffers
        //
        private byte _cpOutData;        // OUT from IOP (CP reads)
        private byte _cpInData;         // IN from CP (IOP reads)

        // Used as TPC address when IOP is writing control store or modifying TPC values.
        private int _tpcAddr;

        // Temporary used when loading TPC values; stores high bits of new TPC address.
        private int _tpcTemp;
    }
}
