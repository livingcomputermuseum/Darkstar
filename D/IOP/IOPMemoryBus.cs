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
using System.IO;

namespace D.IOP
{
    /// <summary>
    /// Implements the IOP's memory bus.  Memory map looks like
    /// (see SysDefs.asm):
    /// 
    /// $0000 - $1FFF   :  PROM (8K)
    /// $2000 - $5FFF   :  RAM  (16K)
    /// $80B0 - $80BF   :  Host Addr PROM (16 bytes)
    ///
    /// </summary>
    public class IOPMemoryBus : I8085MemoryBus
    {
        public IOPMemoryBus(I8085IOBus ioBus)
        {
            // 8K ROM
            _rom = new byte[0x2000];

            // 16K RAM.  We actually allocate 24K
            // to make addressing simpler.
            _ram = new byte[0x6000];

            // The 8085's IO ports are also memory-mapped at
            // $8000 + port.
            _io = ioBus;

            LoadPROMs();

            LoadHostIDProm();
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x2000)
            {
                return _rom[address];
            }
            else if(address < 0x6000)
            {
                return _ram[address];
            }
            else if(address > 0x80af && address < 0x80c0)
            {
                //
                // Host Address PROM.  This contains 12 nybbles of data containing the 48-bit
                // Ethernet host address and a checksum.
                // The PROM itself is 16 bytes in size.
                //
                return _hostIdProm[address - 0x80b0];
            }
            else if(address > 0x8000 && address < 0x8100)
            {
                // Memory-mapped I/O ports
                if (Log.Enabled) Log.Write(LogComponent.IOPMemory, "Memory-mapped I/O read from {0:x4}", address);
                return _io.In((byte)address);
            }
            else
            {
                // Nothing mapped here.
                if (Log.Enabled) Log.Write(LogComponent.IOPMemory, "Read from nonexistent memory at {0:x4}", address);
                return 0xff;
            }
        }

        public void WriteByte(ushort address, byte b)
        {
            if (address < 0x2000)
            {
                // ROM, not writeable.
            }
            else if (address < 0x6000)
            {
                _ram[address] = b;
            }
            else if (address > 0x8000 && address < 0x8100)
            {
                // Memory-mapped I/O ports
                if (Log.Enabled) Log.Write(LogComponent.IOPMemory, "Memory-mapped I/O write at {0:x4}", address);
                _io.Out((byte)address, b);
            }
            else
            {
                // Nothing mapped here.
                if (Log.Enabled) Log.Write(LogComponent.IOPMemory, "Write to nonexistent memory at {0:x4}", address);
            }
        }

        public ushort ReadWord(ushort address)
        {
            return (ushort)(ReadByte(address) | (ReadByte((ushort)(address + 1)) << 8));
        }

        public void WriteWord(ushort address, ushort u)
        {
            WriteByte(address++, (byte)u);
            WriteByte(address, (byte)(u >> 8));
        }

        public void UpdateHostIDProm()
        {
            LoadHostIDProm();
        }

        private void LoadPROMs()
        {
            // Loads PROMs into ROM space.  The files for rev 3.1 are:
            //  U129 - 537P03029        - $0000
            //  U130 - 537P03030        - $0800
            //  U131 - 537P03700        - $1000
            //  U132 - 537P03032        - $1800

            LoadPROM("537P03029.bin", 0x0000);
            LoadPROM("537P03030.bin", 0x0800);
            LoadPROM("537P03700.bin", 0x1000);
            LoadPROM("537P03032.bin", 0x1800);
        }

        private void LoadPROM(string promName, ushort address)
        {
            string promPath;

            if (!string.IsNullOrWhiteSpace(StartupOptions.RomPath))
            {
                promPath = Path.Combine(StartupOptions.RomPath, promName);
            }
            else
            {
                promPath = Path.Combine("IOP", "PROM", promName);
            }

            try
            {
                using (FileStream promStream = new FileStream(promPath, FileMode.Open, FileAccess.Read))
                {
                    if (promStream.Length != 0x800)
                    {
                        throw new InvalidOperationException(
                            String.Format("PROM file {0} has unexpected size 0x{1:x}", promName, promStream.Length));
                    }

                    promStream.Read(_rom, address, 0x800);
                }
            }
            catch(FileNotFoundException e)
            {
                throw new FileNotFoundException(
                    String.Format("PROM file {0} was not found in directory {1}", promName, promPath),
                    e);
            }
        }

        private void LoadHostIDProm()
        {
            //
            // Copy data from the current emulator config into the HostID prom array.
            //
            for (int i = 0; i < 6; i++)
            {
                byte val = (byte)(Configuration.HostID >> (5 - i) * 8);
                SetIDPromByte(i, val);
            }

            //
            // Calculate the checksum of the PROM.  Looking at the PROM as 
            // 8 bytes (big-endian) rather than 16 nibbles, this is a cyclic XOR of:
            // Bytes 0 & 1 - XOR'd together
            // Bytes 2-5
            // Checksum is stored in Byte 6, complemented checksum is in Byte 7.
            // Byte 8 appears to be unused.
            //
            byte checksum = RotateLeft((byte)(GetIDPromByte(0) ^ GetIDPromByte(1)));

            for (int i = 2; i < 6; i++)
            {
                checksum ^= GetIDPromByte(i);
                checksum = RotateLeft(checksum);
            }

            SetIDPromByte(6, checksum);
            SetIDPromByte(7, (byte)(~checksum));
        }

        private byte GetIDPromByte(int byteNumber)
        {
            return (byte)((_hostIdProm[byteNumber * 2] & 0xf) | (_hostIdProm[byteNumber * 2 + 1] << 4));
        }

        private void SetIDPromByte(int byteNumber, byte value)
        {
            _hostIdProm[byteNumber * 2] = (byte)(value & 0xf);
            _hostIdProm[byteNumber * 2 + 1] = (byte)(value >> 4);
        }

        private byte RotateLeft(byte value)
        {
            return (byte)((value << 1) | ((value & 0x80) != 0 ? 1 : 0));
        }

        private byte[] _rom;
        private byte[] _ram;

        //
        // Host ID Prom: contains the host's Ethernet MAC + checksum.
        // 16 nybbles long.
        private byte[] _hostIdProm = new byte[16];

        private I8085IOBus _io;
    }
}
