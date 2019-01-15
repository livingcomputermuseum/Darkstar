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

namespace D.IOP
{
    /// <summary>
    /// Stub implementation of the Printer port.  This is just
    /// enough to get the rigid diagnostic set to pass.
    /// </summary>
    public class Printer : IIOPDevice
    {
        public Printer()
        {
            Reset();
        }

        public void Reset()
        {
            _rxRequest = true;      // active low
            _txRequest = true;      // always ready to transmit, makes the diags happy.
        }

        public bool RxRequest
        {
            get { return _rxRequest; }
        }

        public bool TxRequest
        {
            get { return _txRequest; }
        }

        public int[] ReadPorts
        {
            get { return _readPorts; }
        }

        public int[] WritePorts
        {
            get { return _writePorts; }
        }

        public byte ReadPort(int port)
        {
            byte value = 0;
            switch(port)
            {
                case 0x88:
                    if (Log.Enabled) Log.Write(LogComponent.IOPPrinter, "Stub: Printer data port read.");
                    value = _txData;        // just loopback data to make diags happy.
                    break;

                case 0x89:
                    if (Log.Enabled) Log.Write(LogComponent.IOPPrinter, "Stub: Printer status port read.  Returning DTR");
                    value = 0x00;
                    _rxRequest = true;
                    break;
            }

            return value;
        }

        public void WritePort(int port, byte data)
        {
            switch(port)
            {
                case 0x88:
                    if (Log.Enabled) Log.Write(LogComponent.IOPPrinter, "Stub: Printer data port write 0x{0:x2}.", data);
                    _txData = data;
                    _rxRequest = false;     // "TTY request is active low."
                    break;

                case 0x89:
                    if (Log.Enabled) Log.Write(LogComponent.IOPPrinter, "Stub: Printer control port write 0x{0:x2}.", data);
                    break;
            }
        }

        private bool _rxRequest;
        private bool _txRequest;

        private byte _txData;

        private readonly int[] _readPorts = new int[]
           {
                0x88,       // data
                0x89,       // status
           };

        private readonly int[] _writePorts = new int[]
            {
                0x88,       // data
                0x89,       // commands
            };
    }
}
