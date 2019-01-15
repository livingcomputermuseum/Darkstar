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

namespace D.Memory
{
    public class MemoryController
    {
        public MemoryController()
        {
            _mem = new Memory();            
        }

        public void Reset()
        {
            _mStatus = 0;
            _mar = 0;
            _md = 0;
            _mdValid = true;

            _mem.Reset();
        }

        public ushort MStatus
        {
            get { return _mStatus; }
        }

        public int MAR
        {
            get { return _mar; }
        }

        // Exposing memory for debugging purposes.
        public Memory DebugMemory
        {
            get { return _mem; }
        }

        /// <summary>
        /// Used for debugging.
        /// </summary>
        public ushort MD
        {
            get { return _md; }
        }

        public void SetMCtl(ushort value)
        {
            //
            // See HWRef, figure 18 (pg 55).
            // This is used to test syndrome bits or clear error logs.
            //
            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.MemoryControl, "MCtl<-0x{0:x}", value);

            _mem.SetCheckBits((value & 0xff) >> 2);

            if ((value & 0x800) != 0)
            {
                // Clear error log for the task specified by bits [5..7].
                int task = (value & 0x700) >> 8;
                _mStatus &= (ushort)(~(0x80 >> task));
            }
        }

        public void LoadMAR(int address)
        {
            _mar = address;

            //
            // Pre-load the memory requested, so we can return
            // the original data in cases where MDR is written and MD is read in the same click.
            //
            _md = _mem.ReadWord(_mar, out _mdValid);

            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.MemoryAccess, "MAR<-0x{0:x5}", address);
        }

        public void LoadMDR(ushort value)
        {
            //
            // Write the word to memory
            //
            _mem.WriteWord(_mar, value);
            
            if (Log.Enabled)
            {
                Log.Write(LogType.Verbose, LogComponent.MemoryAccess, "MDR<-0x{0:x4}", value);
                if (_mar >= 0x10000 && _mar < 0x20000)
                {
                    Log.Write(LogType.Verbose, LogComponent.CPMap, "MAP 0x{0:x5} = {1:x4}", _mar, value);
                }
            }
        }

        public ushort ReadMD(TaskType task, out bool valid)
        {
            //
            // Return the data read in LoadMAR.
            //
            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.MemoryAccess, "<-MD (0x{0:x4}) valid {1}", _md, _mdValid);
                
            valid = _mdValid;

            if (!valid)
            {
                if (Log.Enabled) Log.Write(LogComponent.MemoryAccess, "Read from nonexistent memory address 0x{0:x}", _mar);
                _mStatus |= (ushort)((0x80 >> (int)task));  // Set error bit for task
                _mStatus |= 0x100; // double-bit error.

                // TODO: set syndrome bits
            }
            else
            {
                // Clear single/double-bit errors (bits 6..7)
                _mStatus &= 0xfcff;
            }
            return _md;
        }

        private int _mar;
        private ushort _md;
        private bool _mdValid;
        private ushort _mStatus;

        private Memory _mem;
    }
}
