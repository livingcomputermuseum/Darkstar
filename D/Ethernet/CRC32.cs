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


namespace D.Ethernet
{
    public class CRC32
    {
        static CRC32()
        {
            //
            // Initialize the CRC table.
            //
            uint temp = 0;
            for (uint i = 0; i < _crcTable.Length; i++)
            {
                temp = i;

                for (int j = 8; j > 0; j--)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (uint)((temp >> 1) ^ _polynomial);
                    }
                    else
                    {
                        temp >>= 1;
                    }
                }

                _crcTable[i] = temp;
            }
        }

        public CRC32()
        {
            Reset();
        }

        public uint Checksum
        {
            get { return ~_checksum; }
        }

        public void Reset()
        {
            _checksum = 0xffffffff;
        }

        public void AddToChecksum(ushort word)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(word >> 8);
            bytes[1] = (byte)word;
            
            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)((_checksum ^ bytes[i]) & 0xff);
                _checksum = (_checksum >> 8) ^ _crcTable[index];
            }
        }

        private uint _checksum;

        private static uint[] _crcTable = new uint[256];
        private const uint _polynomial = 0xedb88320;
    }
   
}
