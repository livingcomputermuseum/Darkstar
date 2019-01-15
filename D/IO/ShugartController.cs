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


using D.CP;
using D.Logging;
using System;
using System.Collections.Generic;

namespace D.IO
{
    /// <summary>
    /// Implements the hardware end of the Shugart disk controller, currently only supplying the logic
    /// for an SA1000-style drive.
    /// </summary>
    public class ShugartController
    {
        public ShugartController(DSystem system, SA1000Drive drive)
        {
            _system = system;
            _drive = drive;

            _writePipeline = new Queue<ushort>();
        }

        public void Reset()
        {
            _writeEnable = false;
            _wakeupControl = 0;
            _writeCRC = false;
            _transferEnable = false;
            _firmwareEnable = false;
            _directionIn = false;
            _step = false;
            _reduceIW = false;
            _faultClear = false;
            _driveSelect = false;
            _headSelect = 0;
            _wakeupRequest = ServiceRequest.NoWakeup0;

            _verifyError = false;
            _crcError = false;
            _overrun = false;
            _writeFault = false;
            _sa1000 = true;
            _sectorFound = false;
            _indexFound = false;
            _readWordReady = false;

            _writePipeline.Clear();

            ResetTransfer();
        }

        public void SetKCtl(ushort value)
        {
            _writeEnable = (value & 0x0001) != 0;
            _wakeupControl = (value & 0x0006) >> 1;
            _writeCRC = (value & 0x0008) != 0;
            _transferEnable = (value & 0x0010) != 0;
            _firmwareEnable = (value & 0x0020) != 0;
            _directionIn = (value & 0x0040) != 0;
            _step = (value & 0x0080) != 0;
            _reduceIW = (value & 0x0100) != 0;
            _faultClear = (value & 0x0200) != 0;
            _driveSelect = (value & 0x0400) != 0;
            _headSelect = (value & 0xf800) >> 11;

            _wakeupRequest = (ServiceRequest)(_wakeupControl | (_transferEnable ? 0x4 : 0x0));

            //
            // "The SA1000's WriteFault is cleared by de-selecting the drive for at least 500ns."
            // We're not that picky.
            //
            if (!_driveSelect)
            {
                _writeFault = false;
            }

            //
            // Step the drive
            //            
            _drive.Step(_directionIn, _step);

            // Select the head
            //
            _drive.SetHead(_headSelect);

            //
            // Handle wakeup requests that might be pending based on the control value.
            //
            bool wake = false;
            switch (_wakeupRequest)
            {
                case ServiceRequest.FirmwareEnable:
                    wake = _firmwareEnable;
                    break;

                case ServiceRequest.SeekComplete:
                    wake = SeekComplete();
                    break;

                case ServiceRequest.IndexFound:
                    wake = _indexFound;
                    break;

                case ServiceRequest.SectorFound:
                    wake = _sectorFound;
                    break;

                default:
                    wake = false;
                    break;
            }

            if (_transferEnable)
            {
                if (_writeEnable && _wakeupRequest == ServiceRequest.WriteWordNeeded)
                {
                    _transfer = TransferType.Write;
                }
                else if (!_writeEnable && _wakeupRequest == ServiceRequest.WriteWordNeeded)
                {
                    _transfer = TransferType.Verify;
                }
                else if (!_writeEnable && _transferEnable && _wakeupRequest == ServiceRequest.ReadWordReady)
                {
                    _transfer = TransferType.Read;
                }
                else
                {
                    throw new InvalidOperationException("Unexpected combination of service request and write enable flags");
                }
            }
            else
            {
                //
                // Reset transfer state machine if _transferEnable is low.
                //                
                ResetTransfer();
            }

            if (wake)
            {
                _system.CP.WakeTask(TaskType.Disk);
            }
            else
            {
                _system.CP.SleepTask(TaskType.Disk);
            }

            /*
            if (Log.Enabled) Log.Write(
                LogType.Verbose,
                LogComponent.ShugartControl,
                "KCtl<-0x{0:x4} : we {1} wc {2} wcrc {3} te {4} fe {5} di {6} st {7} ri {8} fc {9} ds {10} hs {11} wq {12}",
                value,
                _writeEnable,
                _wakeupControl,
                _writeCRC,
                _transferEnable,
                _firmwareEnable,
                _directionIn,
                _step,
                _reduceIW,
                _faultClear,
                _driveSelect,
                _headSelect,
                _wakeupRequest); */
        }

        public void SetKCmd(ushort value)
        {
            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.ShugartControl, "KCmd<-0x{0:x4} unimplemented.", value);
        }

        public void ClrKFlags()
        {
            // This depends on the kind of service request currently set.
            // TODO: figure out exactly what this works out to...
            if (_wakeupRequest != ServiceRequest.FirmwareEnable && _wakeupRequest != ServiceRequest.SeekComplete)
            {
                _system.CP.SleepTask(TaskType.Disk);
            }

            _verifyError = false;
            _crcError = false;
            _overrun = false;
            _indexFound = false;
            _sectorFound = false;

            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.ShugartControl, "ClrKFlags");
        }

        public void KStrobe()
        {
            if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.ShugartControl, "KStrobe unimplemented");
        }

        public ushort ReadKStatus()
        {
            // if (Log.Enabled) Log.Write(LogType.Warning, LogComponent.ShugartControl, "<-KStatus read");

            // "All status bits are inverted on the X bus because use of the comparable non-inverting drivers
            //  was forbidden when the board was designed."
            ushort value = (ushort)~(
                (_verifyError ? 0x0001 : 0x0000) |
                (_crcError ? 0x0002 : 0x0000) |
                (_overrun ? 0x0004 : 0x0000) |
                (_writeFault ? 0x0008 : 0x0000) |
                (!_drive.IsReady ? 0x0010 : 0x0000) |
                (_sa1000 ? 0x0020 : 0x0000) |
                (!_sectorFound ? 0x0040 : 0x0000) |
                (_indexFound ? 0x0080 : 0x0000) |
                (_firmwareEnable ? 0x0100 : 0x0000) |
                (_drive.Track0 ? 0x0200 : 0x0000) |
                (SeekComplete() ? 0x0400 : 0x0000) |
                ((~_headSelect & 0x1f) << 11)
                );

            /*
            if (Log.Enabled) Log.Write(
                LogType.Verbose,
                LogComponent.ShugartControl,
                "0x{0:x4}<-KStatus",
                value); */

            return value;
        }

        public ushort ReadKTest()
        {
            //
            // This is only partially implemented, enough to indicate to the microcode what kind of drive is
            // attached.
            //
            // Drive type is exposed via KTest[9] (Sector').
            // For an SA1000 this is always 0.
            // For a Quantum Q2040, this is always 1.
            // For a Quantum Q2080, this is the inverse of the MSB of the head select (KCtl[0]).
            //
            
            int value = 0;

            switch (_drive.Type)
            {
                case DriveType.SA1004:
                    value = 0;
                    break;

                case DriveType.Q2040:
                    value = 0x40;
                    break;

                case DriveType.Q2080:
                    value = ((~_headSelect) & 0x10) == 0 ? 0x40 : 0;
                    break;
            }

            return (ushort)value;
        }

        public void SetKOData(ushort value)
        {
            if (_writePipeline.Count > 1)
            {
                if (Log.Enabled) Log.Write(LogType.Error,
                    LogComponent.ShugartControl,
                    "KOData<- : Not ready for write! (c/h/s/f {0}/{1}/{2}/{3})",
                    _drive.Cylinder,
                    _drive.Head,
                    _debugSector,
                    _debugField);
            
            }
            else
            {
                _writePipeline.Enqueue(value);
                if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "KOData<- : 0x{0:x4} latched", value);
            }

            if (_wakeupRequest == ServiceRequest.WriteWordNeeded)
            {
                _system.CP.SleepTask(TaskType.Disk);
            }

        }

        public ushort ReadKIData()
        {
            if (!_readWordReady)
            {
                if (Log.Enabled) Log.Write(LogType.Error,
                    LogComponent.ShugartControl,
                    "<-KIData : Not ready for read! (c/h/s/f {0}/{1}/{2}/{3})",
                    _drive.Cylinder,
                    _drive.Head,
                    _debugSector,
                    _debugField);
                _overrun = true;
            }

            //
            // Put the microcode back to sleep now that we've read the next word.
            //
            if (_wakeupRequest == ServiceRequest.ReadWordReady)
            {
                _system.CP.SleepTask(TaskType.Disk);
            }

            _readWordReady = false;

            // if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "<-KIData Read 0x{0:x4} from c/h/w {1}/{2}/{3} (sector {4} field {5})", _readData, _drive.Cylinder, _drive.Head, _drive.WordIndex, _debugSector, _debugField);

            return (ushort)_readData;
        }

        /// <summary>
        /// Called by the drive to let the controller know that a new word is ready to be read or written.
        /// </summary>
        public void SignalDiskWordReady()
        {
            if (_drive.Index)
            {
                _indexFound = true;

                // Wake up if we're waiting for the Index
                if (_wakeupRequest == ServiceRequest.IndexFound)
                {
                    _system.CP.WakeTask(TaskType.Disk);
                }

                //
                // Reset debug counters
                //
                _debugSector = -1;
                _debugField = -1;
            }

            // Check for overrun on reads
            //
            if (_readWordReady &&
                _transferEnable &&
                _transfer == TransferType.Read &&
                _readState == ReadState.Data)
            {
                // No data available from microcode.
                if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Read data: overrun.");
                _overrun = true;
            }

            _readWordReady = true;
            _readData = _drive.ReadData();

            //
            // Check for Header Address Mark and set SectorFound bit if present.            
            if (_readData == _headerAddressMark)
            {
                _sectorFound = true;

                if (_debugSector != -1 && _debugField != 2)
                {
                    //Console.WriteLine("Only found {0} fields on the last sector.", _debugField);
                }

                _debugSector++;     // next sector
                _debugField = 0;    // header field
            }

            if (_readData == _labelDataAddressMark)
            {
                _debugField++;  // next field
            }

            //
            // Run the transfer state machine
            //
            if (_transferEnable)
            {
                bool wake = false;
                switch (_transfer)
                {
                    case TransferType.Write:
                        switch (_writeState)
                        {
                            case WriteState.AutoPreamble:
                                // One word of preamble is written by the hardware itself.                                
                                _drive.WriteData(0);

                                _transferCount++;
                                if (_transferCount > 1)
                                {
                                    _transferCount = 0;
                                    _writeState = WriteState.Preamble;

                                    // Wake the microcode task for the first preamble word
                                    wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;
                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Preamble word needed.");
                                }
                                break;

                            case WriteState.Preamble:
                                // Four words of preamble are to be written by the microcode.                               
                                if (_writePipeline.Count > 0)
                                {
                                    _drive.WriteData(_writePipeline.Dequeue());
                                }
                                else
                                {
                                    // No data available from microcode.
                                    if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Write preamble: overrun.");
                                    _overrun = true;
                                }

                                // Wake the microcode task
                                wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;

                                _transferCount++;
                                if (_transferCount > 4)
                                {
                                    _transferCount = 0;
                                    _writeState = WriteState.AddressMark;
                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "AM word needed.");
                                }
                                else
                                {
                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Preamble word needed.");
                                }
                                break;

                            case WriteState.AddressMark:
                                //
                                // One word of address mark, written by the microcode.
                                //                                
                                ushort amWord = 0;

                                if (_writePipeline.Count > 0)
                                {
                                    amWord = _writePipeline.Dequeue();
                                }
                                else
                                {
                                    // No data available from microcode.
                                    if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Write am data: overrun.");
                                    _overrun = true;
                                }

                                _drive.WriteAddressMark(amWord);

                                if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "KOData<- : Address mark is 0x{0:x4}", amWord);

                                wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;
                                _writeState = WriteState.Data;
                                break;

                            case WriteState.Data:
                                //
                                // N words of data, written by the microcode.  This state is left only when KCtl<- is set to enable
                                // writing the CRC.
                                //
                                if (_writeCRC)
                                {
                                    wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;
                                    _writeState = WriteState.CRC;
                                    _transferCount = 0;

                                    // Write the first word of the CRC.
                                    _drive.WriteCRC(0xbeef);
                                }
                                else
                                {
                                    wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;

                                    _transferCount++;

                                    if (_writePipeline.Count > 0)
                                    {                  
                                        _drive.WriteData(_writePipeline.Dequeue());
                                    }
                                    else
                                    {
                                        // No data available from microcode.
                                        if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Write data: overrun.");
                                        _overrun = true;
                                    }

                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Data word needed.");
                                }                                
                                break;

                            case WriteState.CRC:
                                //
                                // One word of CRC, written by the hardware.  This takes three word times, but
                                // only one word is written.
                                // We don't actually calculate the CRC (it cannot be read by microcode and
                                // we aren't simulating corrupted disks), but we write a known
                                // value for basic sanity checking on reads.
                                //
                                
                                //
                                // The actual CRC was written when we made the state transition.
                                // We write these other marker words to clobber anything that might
                                // be underneath them.
                                //
                                _drive.WriteCRC(0xdead);
                                
                                _transferCount++;

                                // We still keep the microcode alive -- it may write words; we will ignore them.
                                wake = _wakeupRequest == ServiceRequest.WriteWordNeeded;

                                if (_transferCount > 1)
                                {
                                    _writeState = WriteState.Complete;
                                }
                                break;

                            case WriteState.Complete:
                                // Nothing.
                                break;

                        }
                        break;

                    case TransferType.Verify:
                        switch (_verifyState)
                        {
                            case VerifyState.WaitForAddressMark:
                                // Check the current data under the read heads, if it's an address mark,
                                // wake the Disk task up and move to the Data state.
                                if (_readData == _headerAddressMark ||
                                    _readData == _labelDataAddressMark)
                                {
                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Address Mark 0x{0:x4} found at word 0x{1:x4}, waking microcode.",
                                        _readData,
                                        _drive.WordIndex);
                                    wake = true;
                                    _verifyState = VerifyState.Data;

                                    //
                                    // Set the CRC error flag:
                                    // This is presumed true until all words of the field
                                    // are read and can be compared with the CRC word at the
                                    // end.
                                    //
                                    _crcError = true;

                                    if (_writePipeline.Count == 0)
                                    {
                                        //
                                        // Hack: boot microcode does not "prime" the write pipeline --
                                        // relying on the hardware behavior of clearing the writeData buffer
                                        // automatically; if the microcode hasn't written a word by this point
                                        // enqueue a zero.
                                        //
                                        _writePipeline.Enqueue(0);
                                    }
                                }
                                break;

                            case VerifyState.Data:
                                //
                                // If this is a CRC, we check for our canard value and move to the CRC state,
                                // otherwise compare data.
                                //
                                ushort writeData = 0;
                                if (_writePipeline.Count > 0)
                                {
                                    writeData = _writePipeline.Dequeue();
                                }

                                if ((_readData & 0x20000) != 0)
                                {
                                    if (_readData != _fakeCRCValue)
                                    {
                                        if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Verify CRC: 0x{0:x4} != 0x{1:x4}", _fakeCRCValue, _readData);
                                        _crcError = true;
                                    }
                                    else
                                    {
                                        //
                                        // All good.
                                        //
                                        _crcError = false;
                                    }

                                    _verifyState = VerifyState.CRC;
                                }
                                else
                                {                                    
                                    if (writeData != _readData)
                                    {
                                        if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Verify data: 0x{0:x4} != 0x{1:x4}", writeData, _readData);
                                        _verifyError = true;
                                    }
                                }
                                wake = true;
                                break;

                            case VerifyState.CRC:
                                //
                                // We sit here forever.  Microcode may send two extra words which we will ignore,
                                // after which it will disable the controller.
                                //
                                if (_writePipeline.Count > 0)
                                {
                                    _writePipeline.Dequeue();
                                }
                                wake = true;
                                break;
                        }

                        break;

                    case TransferType.Read:
                        switch (_readState)
                        {
                            case ReadState.WaitForAddressMark:
                                // Check the current data under the read heads, if it's an address mark,
                                // wake the Disk task up and move to the Data state.
                                if (_readData == _headerAddressMark ||
                                    _readData == _labelDataAddressMark)
                                {
                                    if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Address Mark 0x{0:x4} found at word 0x{1:x4}, waking microcode.",
                                        _drive.ReadData(),
                                        _drive.WordIndex);
                                    wake = true;
                                    _readState = ReadState.Data;
                                }
                                break;

                            case ReadState.Data:
                                //
                                // If this is a CRC, we check for our canard value and move to the CRC state,
                                // otherwise the microcode will read the next word via <-KIData
                                //
                                if ((_readData & 0x20000) != 0)
                                {
                                    if (_readData != _fakeCRCValue)
                                    {
                                        if (Log.Enabled) Log.Write(LogType.Error, LogComponent.ShugartControl, "Read CRC: 0x{0:x4} != 0x{1:x4}", _fakeCRCValue, _readData);
                                        _crcError = true;
                                    }

                                    _readState = ReadState.CRC;

                                }
                                wake = true;
                                break;

                            case ReadState.CRC:
                                //
                                // We sit here forever.  Microcode may send two extra words which we will ignore,
                                // after which it will disable the controller.
                                //
                                if (_writePipeline.Count > 0)
                                {
                                    _writePipeline.Dequeue();
                                }
                                wake = true;
                                break;
                        }

                        break;
                }

                //
                // Wake up the disk task if we need the microcode to read or write a word.
                //
                if (wake)
                {
                    _system.CP.WakeTask(TaskType.Disk);
                }

            }
        }

        public void SignalSeekComplete()
        {
            if (_wakeupRequest == ServiceRequest.SeekComplete && SeekComplete())
            {
                _system.CP.WakeTask(TaskType.Disk);
            }
        }

        private bool SeekComplete()
        {
            //
            // "[SeekComplete] is set when the drive is ready, it is selected, and the heads are not in motion."
            //
            return (_drive.IsReady && _driveSelect && _drive.SeekComplete);
        }

        private void ResetTransfer()
        {
            _transfer = TransferType.None;
            _writeState = WriteState.AutoPreamble;
            _verifyState = VerifyState.WaitForAddressMark;
            _readState = ReadState.WaitForAddressMark;
            _transferCount = 0;
            _writePipeline.Clear();
        }

        private DSystem _system;

        //
        // The drive the controller is connected to.
        //
        private SA1000Drive _drive;

        //
        // KCtl bits
        //
        private bool _writeEnable;
        private int _wakeupControl;
        private bool _writeCRC;
        private bool _transferEnable;
        private bool _firmwareEnable;
        private bool _directionIn;
        private bool _step;
        private bool _reduceIW;
        private bool _faultClear;
        private bool _driveSelect;
        private int _headSelect;

        private ServiceRequest _wakeupRequest;

        //
        // KStatus bits
        //
        private bool _verifyError;
        private bool _crcError;
        private bool _overrun;
        private bool _writeFault;
        private bool _sa1000;
        private bool _sectorFound;
        private bool _indexFound;


        //
        // Read/Write state machine
        //        
        private int _transferCount;
        private uint _readData;
        private bool _readWordReady;

        //
        // Write pipeline (word being actively written to disk, word loaded by KOData<-.
        // Kind of overkill to use a Queue object here.
        //
        private Queue<ushort> _writePipeline = new Queue<ushort>();

        private WriteState _writeState;
        private VerifyState _verifyState;
        private ReadState _readState;
        private TransferType _transfer;

        //
        // Address marks
        //
        private const int _headerAddressMark = 0x1a141;
        private const int _labelDataAddressMark = 0x1a143;

        //
        // CRC (not actually CRC) value
        //
        private const int _fakeCRCValue = 0x2beef;

        //
        // Debug metadata.  Keep track of sector and field information.
        //
        private int _debugSector;
        private int _debugField;

        private enum TransferType
        {
            None,
            Read,
            Write,
            Verify
        }

        private enum ServiceRequest
        {
            FirmwareEnable = 0,
            SeekComplete = 1,
            IndexFound = 2,             // NB: HWRef has IndexFound and SectorFound values reversed.
            SectorFound = 3,
            ReadWordReady = 4,
            WriteWordNeeded = 5,
            NoWakeup0 = 6,
            NoWakeup1 = 7,
        }

        private enum WriteState
        {
            Invalid = 0,
            AutoPreamble,           // 2 words of zeros written by the hardware
            Preamble,               // 4 words of zeros written by the microcode
            AddressMark,            // 1 word of address mark written by the microcode
            Data,                   // 256 words actual sector data written by the microcode
            CRC,                    // 1 word of CRC, written by the hardware
            Complete,               // write done
        }

        private enum VerifyState
        {
            Invalid = 0,
            WaitForAddressMark,     // Waiting for the next Address Mark to come around
            Data,                   // Data is being read and compared with data from the microcode, by the hardware
            CRC,                    // CRC is being checked by the hardware
        }

        private enum ReadState
        {
            Invalid = 0,
            WaitForAddressMark,     // Waiting for the next Address Mark to come around
            Data,                   // Data is being read
            CRC,                    // CRC is being checked by the hardware
        }

    }
}
