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


#define LOGGING_ENABLED

using System;
using System.IO;

namespace D.Logging
{
    /// <summary>
    /// Specifies a component to specify logging for
    /// </summary>
    [Flags, ]
    public enum LogComponent
    {
        None        = 0,

        // IOP
        IOP         = 0x1,
        IOPMemory   = 0x2,
        IOPIO       = 0x4,
        IOPFloppy   = 0x8,
        IOPMisc     = 0x10,
        IOPDMA      = 0x20,
        IOPKeyboard = 0x40,
        IOPPrinter  = 0x80,

        // CP
        CPControl   = 0x100,
        CPExecution = 0x200,
        CPMicrocodeLoad = 0x400,
        CPTPCLoad = 0x800,
        CPTask      = 0x1000,
        CPMap       = 0x2000,
        CPError     = 0x4000,
        CPIB        = 0x8000,
        CPStack     = 0x10000,
        CPInst      = 0x20000,

        // Memory
        MemoryControl = 0x100000,
        MemoryAccess = 0x200000,

        // Display
        DisplayControl = 0x400000,

        // Shugart controller
        ShugartControl = 0x800000,

        // Ethernet
        EthernetControl = 0x1000000,
        HostEthernet =     0x2000000,
        EthernetTransmit = 0x4000000,
        EthernetReceive =  0x8000000,
        EthernetPacket = 0x10000000,

        // Configuration
        Configuration =   0x40000000,

        All = 0x7fffffff
    }

    /// <summary>
    /// Specifies the type (or severity) of a given log message
    /// </summary>
    [Flags]
    public enum LogType
    {
        None = 0,
        Normal = 0x1,
        Warning = 0x2,
        Error = 0x4,
        Verbose = 0x8,
        All = 0x7fffffff
    }

    /// <summary>
    /// Provides basic functionality for logging messages of all types.
    /// </summary>
    public static class Log
    {
        static Log()
        {
            Enabled = false;
            _components = LogComponent.None;
            _type = LogType.None;
            _logIndex = 0;
        }

        public static LogComponent LogComponents
        {
            get { return _components; }
            set { _components = value; }
        }

        public static readonly bool Enabled;

#if LOGGING_ENABLED
        /// <summary>
        /// Logs a message without specifying type/severity for terseness;
        /// will not log if Type has been set to None.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Write(LogComponent component, string message, params object[] args)
        {
            Write(LogType.Normal, component, message, args);
        }

        public static void Write(LogType type, LogComponent component, string message, params object[] args)
        {
            if ((_type & type) != 0 &&
                (_components & component) != 0)
            {
                //
                // My log has something to tell you...
                // TODO: color based on type, etc.
                Console.WriteLine(_logIndex.ToString() + ": " + component.ToString() + ": " + message, args);
                _logIndex++;
            }
        }
#else
        public static void Write(LogComponent component, string message, params object[] args)
        {
            
        }

        public static void Write(LogType type, LogComponent component, string message, params object[] args)
        {

        }

#endif

        private static LogComponent _components;
        private static LogType _type;
        private static long _logIndex;
    }
}
