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
    public class IOPIOBus : I8085IOBus
    {
        public IOPIOBus()
        {
            _writeDispatch = new IIOPDevice[256];
            _readDispatch = new IIOPDevice[256];
        }

        public void RegisterDevice(IIOPDevice device)
        {
            foreach(byte port in device.ReadPorts)
            {
                if (_readDispatch[port] != null)
                {
                    throw new InvalidOperationException(String.Format("Read port collision {0:x2} when adding device {1}", port, device));
                }

                _readDispatch[port] = device;
            }

            foreach (byte port in device.WritePorts)
            {
                if (_writeDispatch[port] != null)
                {
                    throw new InvalidOperationException(String.Format("Write port collision {0:x2} when adding device {1}", port, device));
                }

                _writeDispatch[port] = device;
            }
        }

        public void Out(byte port, byte val)
        {
            if (_writeDispatch[port] != null)
            {
                _writeDispatch[port].WritePort(port, val);
            }
            else
            {
                if (Log.Enabled) Log.Write(LogComponent.IOPIO, "Unhandled write to IO port ${0:x2}, ${1:x2}", port, val);
            }
        }

        public byte In(byte port)
        {
            if (_readDispatch[port] != null)
            {
                return _readDispatch[port].ReadPort(port);
            }
            else
            {
                if (Log.Enabled) Log.Write(LogComponent.IOPIO, "Unhandled read from IO port ${0:x2}", port);
                return 0x00;
            }
        }


        private IIOPDevice[] _writeDispatch;
        private IIOPDevice[] _readDispatch;
    }
}
