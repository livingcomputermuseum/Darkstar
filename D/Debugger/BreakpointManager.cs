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
using System.Collections.Generic;

namespace D.Debugger
{
    [Flags]
    public enum BreakpointType
    {
        None = 0,
        Execution = 1,
        Read = 2,
        Write = 4,
    }

    public enum BreakpointProcessor
    {
        IOP = 0,
        CP,
        Mesa,
    }

    public struct BreakpointEntry
    {
        public BreakpointEntry(BreakpointProcessor processor, BreakpointType type, int address)
        {
            Processor = processor;
            Type = type;
            Address = address;
        }

        public BreakpointProcessor Processor;
        public BreakpointType Type;
        public int Address;
    }

    public static class BreakpointManager
    {
        static BreakpointManager()
        {
            // We use a flat array to make lookup as
            // cheap as possible across the various address spaces.
            _iopBreakpoints = new BreakpointType[0x10000];
            _cpBreakpoints = new BreakpointType[0x1000];
            _mesaBreakpoints = new BreakpointType[0x200000];    // 2mb (1mw).
            _enableBreakpoints = true;
        }

        public static bool BreakpointsEnabled
        {
            get { return _enableBreakpoints; }
            set { _enableBreakpoints = value; }
        }

        public static void SetBreakpoint(BreakpointEntry entry)
        {
            BreakpointType[] _bkpts = GetBreakpointsForProcessor(entry.Processor);
                        
            if (entry.Type == BreakpointType.None)
            {
                _bkpts[entry.Address] = BreakpointType.None;
            }
            else
            {
                _bkpts[entry.Address] |= entry.Type;
            }
        }

        public static BreakpointType GetBreakpoint(BreakpointProcessor processor, ushort address)
        {
            BreakpointType[] _bkpts = GetBreakpointsForProcessor(processor);
            return _bkpts[address];
        }

        public static bool TestBreakpoint(BreakpointEntry entry)
        {
            if (!_enableBreakpoints)
            {
                return false;
            }

            BreakpointType[] _bkpts = GetBreakpointsForProcessor(entry.Processor);
            return (_bkpts[entry.Address] & entry.Type) != 0;
        }

        public static bool TestBreakpoint(BreakpointProcessor processor, BreakpointType type, int address)
        {
            if (!_enableBreakpoints)
            {
                return false;
            }

            BreakpointType[] _bkpts = GetBreakpointsForProcessor(processor);
            return (_bkpts[address] & type) != 0;
        }

        public static List<BreakpointEntry> EnumerateBreakpoints()
        {
            List<BreakpointEntry> breakpoints = new List<BreakpointEntry>();

            for (ushort i = 0; i < _iopBreakpoints.Length; i++)
            {
                if (_iopBreakpoints[i] != BreakpointType.None)
                {
                    breakpoints.Add(new BreakpointEntry(BreakpointProcessor.IOP, _iopBreakpoints[i], i));
                }                
            }

            for (ushort i = 0; i < _cpBreakpoints.Length; i++)
            {
                if (_cpBreakpoints[i] != BreakpointType.None)
                {
                    breakpoints.Add(new BreakpointEntry(BreakpointProcessor.CP, _cpBreakpoints[i], i));
                }
            }

            for (ushort i = 0; i < _mesaBreakpoints.Length; i++)
            {
                if (_mesaBreakpoints[i] != BreakpointType.None)
                {
                    breakpoints.Add(new BreakpointEntry(BreakpointProcessor.Mesa, _mesaBreakpoints[i], i));
                }
            }

            return breakpoints;
        }

        private static BreakpointType[] GetBreakpointsForProcessor(BreakpointProcessor processor)
        {
            switch (processor)
            {
                case BreakpointProcessor.CP:
                    return _cpBreakpoints;                    

                case BreakpointProcessor.IOP:
                    return _iopBreakpoints;                    

                case BreakpointProcessor.Mesa:
                    return _mesaBreakpoints;                    
            }

            return null;
        }

        private static BreakpointType[] _iopBreakpoints;
        private static BreakpointType[] _cpBreakpoints;
        private static BreakpointType[] _mesaBreakpoints;
        private static bool _enableBreakpoints;
    }
}
