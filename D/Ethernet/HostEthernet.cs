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


using SharpPcap;
using SharpPcap.WinPcap;
using SharpPcap.LibPcap;
using SharpPcap.AirPcap;
using PacketDotNet;

using System;
using System.Net.NetworkInformation;


using D.Logging;
using PacketDotNet.Utils;
using System.Text;

namespace D.Ethernet
{
    /// <summary>
    /// Represents a host ethernet interface.
    /// </summary>
    public struct EthernetInterface
    {
        public EthernetInterface(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", Name, Description);
        }

        public string Name;
        public string Description;
    }

    /// <summary>
    /// Implements the logic for sending and receiving emulated 10mbit ethernet packets over an actual
    /// ethernet interface controlled by the host operating system.
    /// 
    /// This uses SharpPcap to do the dirty work.
    /// </summary>
    public class HostEthernetEncapsulation : IPacketInterface
    {
        public HostEthernetEncapsulation(string name)
        {
            // Find the specified device by name
            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                if (device is WinPcapDevice)
                {
                    //
                    // We use the friendly name to make it easier to specify in config files.
                    //
                    if (!string.IsNullOrWhiteSpace(((WinPcapDevice)device).Interface.FriendlyName) &&
                        ((WinPcapDevice)device).Interface.FriendlyName.ToLowerInvariant() == name.ToLowerInvariant())
                    {
                        AttachInterface(device);
                        break;
                    }
                }
                else
                {
                    if (device.Name.ToLowerInvariant() == name.ToLowerInvariant())
                    {
                        AttachInterface(device);
                        break;
                    }
                }
            }

            if (_interface == null)
            {
                Log.Write(LogComponent.HostEthernet, "Specified ethernet interface does not exist or is not compatible with Darkstar.");
                throw new InvalidOperationException("Specified ethernet interface does not exist or is not compatible with Darkstar.");
            }

            UpdateSourceAddress();
        }

        public void RegisterReceiveCallback(ReceivePacketDelegate callback)
        {
            _callback = callback;

            // Now that we have a callback we can start receiving stuff.
            Open(true /* promiscuous */, 0);
            BeginReceive();
        }

        public void Shutdown()
        {
            if (_interface != null)
            {
                try
                {
                    if (_interface.Started)
                    {
                        _interface.StopCapture();
                    }
                }
                catch
                {
                    // Eat exceptions.  The Pcap libs seem to throw on StopCapture on
                    // Unix platforms, we don't really care about them (since we're shutting down anyway)
                    // but this prevents debug spew from appearing on the console.
                }
                finally
                {
                    _interface.Close();
                }
            }
        }

        /// <summary>
        /// Sends an array of words over the ethernet.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="hostId"></param> 
        public void Send(ushort[] packet)
        {
            byte[] packetBytes = new byte[packet.Length * 2];
            
            //
            // Do this annoying dance to stuff the ushorts into bytes because this is C#.
            //
            for (int i = 0; i < packet.Length; i++)
            {
                packetBytes[i * 2] = (byte)(packet[i] >> 8);
                packetBytes[i * 2 + 1] = (byte)(packet[i]);
            }           

            ByteArraySegment seg = new ByteArraySegment(packetBytes);

            EthernetPacket p = new EthernetPacket(seg);

            // Send it over the 'net!
            _interface.SendPacket(p);
            

            Log.Write(LogType.Verbose, LogComponent.EthernetReceive, "Packet (length {0}) sent: dst {1} src {2}",
                            packetBytes.Length,
                            p.DestinationHwAddress, 
                            p.SourceHwAddress);

        }

        private void ReceiveCallback(object sender, CaptureEventArgs e)
        {
            //
            // Filter out packets intended for the emulator, forward them on, drop everything else.
            //
            if (e.Packet.LinkLayerType == LinkLayers.Ethernet)
            {
                //
                // We wrap this in a try/catch; on occasion Packet.ParsePacket fails due to a bug
                // in the PacketDotNet library.
                //
                EthernetPacket packet = null;
                try
                {
                    packet = (EthernetPacket)Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data);
                }
                catch (Exception ex)
                {
                    // Just eat this, log a message.
                    Log.Write(LogType.Error, LogComponent.HostEthernet, "Failed to parse incoming packet.  Exception {0}", ex.Message);
                    packet = null;
                }

                if (packet != null)
                {
                    if (!packet.SourceHwAddress.Equals(_10mbitSourceAddress) &&             // Don't recieve packets sent by this emulator.
                        (packet.DestinationHwAddress.Equals(_10mbitSourceAddress) ||        // Filter on packets destined for us or broadcast.
                         packet.DestinationHwAddress.Equals(_10mbitBroadcastAddress)))
                    {
                        Log.Write(LogType.Verbose, LogComponent.HostEthernet, "Packet received: dst {0} src {1}",
                            packet.DestinationHwAddress, packet.SourceHwAddress);

                        /*
                        if (Log.Enabled)
                        {
                            StringBuilder sb = new StringBuilder();
                            int byteNum = 0;
                            StringBuilder dataLine = new StringBuilder();
                            StringBuilder asciiLine = new StringBuilder();
                            dataLine.AppendFormat("000: ");

                            for (int i = 0; i < e.Packet.Data.Length; i++)
                            {
                                dataLine.AppendFormat("{0:x2} ", e.Packet.Data[i]);
                                asciiLine.Append(GetPrintableChar(e.Packet.Data[i]));

                                byteNum++;
                                if ((byteNum % 16) == 0)
                                {
                                    Log.Write(LogComponent.EthernetPacket, "{0} {1}", dataLine.ToString(), asciiLine.ToString());
                                    dataLine.Clear();
                                    asciiLine.Clear();
                                    dataLine.AppendFormat("{0:x3}: ", i + 1);
                                    byteNum = 0;
                                }
                            }

                            if (byteNum > 0)
                            {
                                Log.Write(LogComponent.EthernetPacket, "{0} {1}", dataLine.ToString(), asciiLine.ToString());
                            }

                            Log.Write(LogComponent.EthernetPacket, "");
                        } */
                    }
                    else
                    {
                        // Not for us, discard the packet.
                    }
                }
            }
        }

        private static char GetPrintableChar(byte b)
        {
            char c = (char)b;
            if (char.IsLetterOrDigit(c) ||
                char.IsPunctuation(c) ||
                char.IsSymbol(c))
            {
                return c;
            }
            else
            {
                return '.';
            }
        }

        private void UpdateSourceAddress()
        {
            byte[] macBytes = new byte[6];

            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = (byte)((Configuration.HostID >> ((5 - i) * 8)));
            }

            _10mbitSourceAddress = new PhysicalAddress(macBytes);
            _10mbitBroadcastAddress = new PhysicalAddress(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff });
        }

        private void AttachInterface(ICaptureDevice iface)
        {
            _interface = iface;

            if (_interface == null)
            {
                throw new InvalidOperationException("Requested interface not found.");
            }

            Log.Write(LogComponent.HostEthernet, "Attached to host interface {0}", iface.Name);
        }

        private void Open(bool promiscuous, int timeout)
        {
            if (_interface is WinPcapDevice)
            {
                ((WinPcapDevice)_interface).Open(promiscuous ? OpenFlags.MaxResponsiveness | OpenFlags.Promiscuous : OpenFlags.MaxResponsiveness, timeout);
            }
            else if (_interface is LibPcapLiveDevice)
            {
                ((LibPcapLiveDevice)_interface).Open(promiscuous ? DeviceMode.Promiscuous : DeviceMode.Normal, timeout);
            }
            else if (_interface is AirPcapDevice)
            {
                ((AirPcapDevice)_interface).Open(promiscuous ? OpenFlags.MaxResponsiveness | OpenFlags.Promiscuous : OpenFlags.MaxResponsiveness, timeout);
            }

            Log.Write(LogComponent.HostEthernet, "Host interface opened and receiving packets.");
        }

        /// <summary>
        /// Begin receiving packets, forever.
        /// </summary>
        private void BeginReceive()
        {
            // Kick off receiver.
            _interface.OnPacketArrival += ReceiveCallback;
            _interface.StartCapture();
        }

        private ICaptureDevice _interface;
        private ReceivePacketDelegate _callback;
        
        private PhysicalAddress _10mbitSourceAddress;
        private PhysicalAddress _10mbitBroadcastAddress;
    }
}
