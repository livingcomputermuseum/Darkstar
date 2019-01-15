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

namespace D.Memory
{
    /// <summary>
    /// Encapsulates the physical memory, with enough of an ECC implementation
    /// to allow diagnostics to pass.
    /// </summary>
    public class Memory
    {
        public Memory()
        {
            Reset();
        }

        public void Reset()
        {
            if (_memory == null || _memory.Length != Configuration.MemorySize * 1024)
            {
                _memory = new ushort[Configuration.MemorySize * 1024];
                _ecc = new byte[_memory.Length];
            }
            else if (_memory != null)
            {
                Array.Clear(_memory, 0, _memory.Length);
                Array.Clear(_ecc, 0, _ecc.Length);
            }
        }

        public int Size
        {
            get { return _memory.Length; }
        }

        public ushort ReadWord(int address, out bool valid)
        {
            if (address < _memory.Length)
            {
                ushort memWord = _memory[address];
                valid = _ecc[address] == CalculateECCSyndrome(memWord);
                return memWord;
            }
            else
            {
                valid = false;
                return 0;
            }
        }

        public void WriteWord(int address, ushort value)
        {
            if (address < _memory.Length)
            {
                WriteECC(address, value);
                _memory[address] = value;
            }
            else
            {
                if (Log.Enabled) Log.Write(LogComponent.MemoryAccess, "Write to nonexistent memory address 0x{0:x}", address);
            }
        }

        public void SetCheckBits(int invertBits)
        {
            _mctlInvert = invertBits;
        }

        private byte CalculateECCSyndrome(ushort word)
        {
            //
            // Not actually calculating an ECC syndrome here, as at the moment there are no
            // plans to emulate faulty memory, or the hardware to correct it.
            // 
            return 0;
        }

        private void WriteECC(int address, ushort value)
        {
            //
            // Write the calculated ECC syndrome value with the bits specified by MCtl<-
            // inverted.
            //
            _ecc[address] = (byte)(CalculateECCSyndrome(value) ^ _mctlInvert);
        }

        private ushort[] _memory;
        private byte[] _ecc;

        private int _mctlInvert;
    }
}
