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
    public class DMAChannel
    {
        public DMAChannel()
        {
            Reset();
            Device = null;            
        }

        public void Reset()
        {
            Enabled = false;
            Completed = false;
            ChAddr = 0;
            ChCount = -1;
            Type = DMAType.Invalid;
        }

        public bool Enabled;

        public bool Completed;

        // Channel address
        public ushort ChAddr;

        // Channel data count
        public int ChCount;

        // Channel DMA type
        public DMAType Type;

        // DMA device
        public IDMAInterface Device;
    }

    public enum DMAType
    {
        Verify = 0,
        Write = 1,
        Read = 2,
        Invalid = 3,
    }

    /// <summary>
    /// Defines an interface for DMA exchanges between devices and the DMA controller.
    /// </summary>
    public interface IDMAInterface
    {
        /// <summary>
        /// DMA Request: device request to obtain a DMA cycle from the DMA controller
        /// </summary>
        bool DRQ { get; }

        /// <summary>
        /// Writes a single byte to the device from the DMA controller
        /// </summary>
        /// <param name="value"></param>
        void DMAWrite(byte value);

        /// <summary>
        /// Reads a single byte from the device to the DMA controller
        /// </summary>
        /// <returns></returns>
        byte DMARead();

        /// <summary>
        /// Indicates to a DMA device that a DMA transfer has completed.
        /// </summary>
        void DMAComplete();
    }

    /// <summary>
    /// This implements the general behavior of the Intel 8257 as used in the Star.
    /// It is far from a generic 8257 simulation, it could be made more general-purpose...
    /// </summary>
    public class DMAController : IIOPDevice
    {
        public DMAController(IOProcessor iop)
        {
            _iop = iop;

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new DMAChannel();
            }
        }

        public void RegisterDevice(IDMAInterface device, int channel)
        {
            _channels[channel].Device = device;
        }

        public DMAChannel GetChannel(int i)
        {
            return _channels[i];
        }

        /// <summary>
        /// Hold request.  Goes high when the DMA controller is taking control of the
        /// bus (so the CPU should take a nap.)
        /// </summary>
        public bool HRQ
        {
            get { return _hrq; }
        }

        /// <summary>
        /// Terminal Count: TC is activated when the 14-bit value in the [last] selected channel's
        /// terminal count register equals zero.
        /// </summary>
        public bool TC
        {
            get
            {
                if (_lastSelectedChannel != -1)
                {
                    return _channels[_lastSelectedChannel].ChCount == 0;
                }
                else
                {
                    return false;
                }
            }

        }

        public int[] ReadPorts
        {
            get { return _readPorts; }
        }

        public int[] WritePorts
        {
            get { return _writePorts; }
        }

        public void Reset()
        {            
            _first = true;

            _rotatingPriority = false;
            _extendedWrite = false;
            _tcStop = false;
            _autoLoad = false;

            _lastSelectedChannel = 0;
            _nextToService = 0;

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i].Reset();
            }
        }

        /// <summary>
        /// Executes a single DMA transfer (if there are
        /// any transfers pending).  This takes 4 clock
        /// cycles.
        /// </summary>
        public void Execute()
        {
            //
            // See if there's anything to do.
            //
            int nextChannel = SelectNextChannel();

            //
            // Raise HRQ if so.
            //
            _hrq = nextChannel != -1;

            if (_hrq)
            {                
                DMAChannel c = _channels[nextChannel];

                if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} selected.  {1} bytes to {2}, addr 0x{3:x4}.",
                    nextChannel, c.ChCount, c.Type, c.ChAddr);

                switch (c.Type)
                {
                    case DMAType.Verify:
                        throw new NotImplementedException("DMA Verify not implemented.");

                    case DMAType.Read:
                        // Read byte from memory and transfer to device
                        byte dmaWrite = _iop.Memory.ReadByte(c.ChAddr);
                        c.Device.DMAWrite(dmaWrite);

                        if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "DMA read transfer of byte 0x{0:x2} from address 0x{1:x4}", dmaWrite, c.ChAddr);
                        break;

                    case DMAType.Write:
                        // Read byte from device and transfer to memory.
                        byte dmaRead = c.Device.DMARead();
                        _iop.Memory.WriteByte(c.ChAddr, dmaRead);

                        if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "DMA write transfer of byte 0x{0:x2} to address 0x{1:x4}", dmaRead, c.ChAddr);
                        break;
                }

                // Increment address, decrement counter.
                c.ChAddr++;
                c.ChCount--;

                // If the counter runs out, stop the channel if so enabled.
                if (c.ChCount == 0)
                {
                    if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} completed.", nextChannel);
                    // Stop this crazy thing.
                    if (_tcStop)
                    {
                        c.Enabled = false;                        
                        if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} disabled.", nextChannel);
                    }

                    c.Completed = true;
                    c.Device.DMAComplete();
                }

                _lastSelectedChannel = nextChannel;
            }
        }

        public void WritePort(int port, byte value)
        {
            switch ((DMAPorts)port)
            {
                case DMAPorts.DmaMode:
                    _first = true;

                    _channels[0].Enabled = (value & 0x01) != 0;
                    _channels[1].Enabled = (value & 0x02) != 0;
                    _channels[2].Enabled = (value & 0x04) != 0;
                    _channels[3].Enabled = (value & 0x08) != 0;

                    _rotatingPriority = (value & 0x10) != 0;
                    _extendedWrite = (value & 0x20) != 0;
                    _tcStop = (value & 0x40) != 0;
                    _autoLoad = (value & 0x80) != 0;

                    if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "DMAMode: en: {0},{1},{2},{3} rp {4} ew {5} tc {6} al {7}",
                        _channels[0].Enabled, _channels[1].Enabled, _channels[2].Enabled, _channels[3].Enabled,
                        _rotatingPriority, _extendedWrite, _tcStop, _autoLoad);

                    if (_autoLoad)
                    {
                        throw new NotImplementedException("AutoLoad not yet implemented.");
                    }
                    break;

                case DMAPorts.DmaCh0Addr:
                case DMAPorts.DmaCh1Addr:
                case DMAPorts.DmaCh2Addr:
                case DMAPorts.DmaCh3Addr:
                    {
                        int ch = (port - 0xa0) / 2;
                        if (_first)
                        {
                            _channels[ch].ChAddr = value;
                        }
                        else
                        {
                            _channels[ch].ChAddr = (ushort)(_channels[ch].ChAddr | (value << 8));

                            if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} address set to 0x{1:x4}", ch, _channels[ch].ChAddr);
                        }
                        _first = !_first;
                    }
                    break;

                case DMAPorts.DmaCh0Count:
                case DMAPorts.DmaCh1Count:
                case DMAPorts.DmaCh2Count:
                case DMAPorts.DmaCh3Count:
                    {
                        int ch = (port - 0xa1) / 2;
                        if (_first)
                        {
                            _channels[ch].ChCount = value;
                        }
                        else
                        {
                            // + 1 because the value loaded is the number of bytes-1.
                            _channels[ch].ChCount = (ushort)(_channels[ch].ChCount | ((value & 0x3f) << 8)) + 1;
                            _channels[ch].Type = (DMAType)(value >> 6);

                            if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} count set to 0x{1:x4}", ch, _channels[ch].ChCount);
                            if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "Channel {0} type set to {1}", ch, _channels[ch].Type);
                        }
                        _first = !_first;
                    }
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unexpected write to port {0:x2}", port));
            }
        }

        public byte ReadPort(int port)
        {
            byte value = 0;

            switch ((DMAPorts)port)
            {
                case DMAPorts.DmaStatus:
                    // "The low 4 bits of this register indicate what channels have completed."
                    value = (byte)((_channels[0].Completed ? 0x01 : 0x00) |
                            (_channels[1].Completed ? 0x02 : 0x00) |
                            (_channels[2].Completed ? 0x04 : 0x00) |
                            (_channels[3].Completed ? 0x08 : 0x00));

                    if (Log.Enabled) Log.Write(LogComponent.IOPDMA, "DMAStatus read {0}", value);

                    // TC Status bits are cleared after the status register is read.
                    _channels[0].Completed = false;
                    _channels[1].Completed = false;
                    _channels[2].Completed = false;
                    _channels[3].Completed = false;
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Unexpected read from port {0:x2}", port));
            }

            return value;

        }

        private int SelectNextChannel()
        {
            int nextChannel = -1;

            if (!_rotatingPriority)
            {
                //
                // Select the highest priority channel that has something to do.
                // Channel 0 has the highest priority.
                //
                for (int i = 0; i < 4; i++)
                {
                    if (_channels[i].Enabled && _channels[i].Device != null &&_channels[i].Device.DRQ)
                    {
                        nextChannel = i;
                        break;
                    }
                }
            }
            else
            {
                //
                // Select the next channel in the cycle, if it has something to do.
                //
                for (int i = 0; i < 4; i++)
                {
                    int j = (i + _nextToService) % 4;
                    if (_channels[j].Enabled && _channels[j].Device != null && _channels[j].Device.DRQ)
                    {
                        nextChannel = j;
                        _nextToService = j + 1;
                        break;
                    }
                }
            }

            return nextChannel;
        }

        private readonly int[] _readPorts = new int[]
            {
                (int)DMAPorts.DmaStatus,
            };

        private readonly int[] _writePorts = new int[]
            {
                (int)DMAPorts.DmaCh0Addr,
                (int)DMAPorts.DmaCh0Count,
                (int)DMAPorts.DmaCh1Addr,
                (int)DMAPorts.DmaCh1Count,
                (int)DMAPorts.DmaCh2Addr,
                (int)DMAPorts.DmaCh2Count,
                (int)DMAPorts.DmaCh3Addr,
                (int)DMAPorts.DmaCh3Count,
                (int)DMAPorts.DmaMode,
            };

        // Which address byte to store when loading registers.
        private bool _first;

        private DMAChannel[] _channels = new DMAChannel[4];

        // DMA Mode Flags
        private bool _rotatingPriority;
        private bool _extendedWrite;
        private bool _tcStop;
        private bool _autoLoad;

        // Scheduling
        private int _nextToService;
        private int _lastSelectedChannel;

        private IOProcessor _iop;                

        private enum DMAPorts
        {
            DmaCh0Addr = 0xa0,
            DmaCh0Count = 0xa1,
            DmaCh1Addr = 0xa2,
            DmaCh1Count = 0xa3,
            DmaCh2Addr = 0xa4,
            DmaCh2Count = 0xa5,
            DmaCh3Addr = 0xa6,
            DmaCh3Count = 0xa7,
            DmaMode = 0xa8,     // Write
            DmaStatus = 0xa8,   // Read
        }

        private bool _hrq;       
    }
}
