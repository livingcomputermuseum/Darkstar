/*
    BSD 2-Clause License

    Copyright Dr. Hans-Walter Latz 2020 and Living Computer Museum + Labs 2018
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
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace D.Ethernet
{
	/// <summary>
	/// Packet interface for accessing a Dodo NetHub as network device for Darkstar.
	/// </summary>
	public class NethubInterface : IPacketInterface
	{
    	
		// this is the name of the NetHub network-device in the configuration dialog resp. the configuration file
    	public const string NETHUB_NAME = "[[ Dodo-Nethub ]]";
    	
		private TcpClient client = null;
		private NetworkStream stream = null;
		
		private ulong localAddress;
		private ulong broadcastAddress;
		
		private ReceivePacketDelegate receiver = null;
		private Thread receiverThread = null;
		
		private Byte[] sendBuffer = new byte[1026]; // this assumes that there is only ONE processing thread (which is the case for DarkStar!) 
		
		public NethubInterface()
		{
            client = new TcpClient(Configuration.NetHubHost, Configuration.NetHubPort);
			stream = client.GetStream();
			
			this.localAddress = Configuration.HostID;
			this.broadcastAddress = 0x0000FFFFFFFFFFFF;
		}
		
		/// <summary>
        /// Registers a callback delegate to handle packets that are received.
        /// </summary>
        /// <param name="callback"></param>
        void IPacketInterface.RegisterReceiveCallback(ReceivePacketDelegate callback) {
        	this.stopReceiverThread();
        	this.receiver = callback;
        	this.receiverThread = new Thread(this.PacketReceiver);
        	this.receiverThread.Start();
        }

        /// <summary>
        /// Sends the specified word array over the device.
        /// </summary>
        /// <param name="packet"></param>
        void IPacketInterface.Send(ushort[] packet) {
        	NetworkStream str = this.stream;
        	if (str == null) {
        		return;
        	}
        	
        	int byteLen = packet.Length * 2;
            //(debug) Console.Write("** sending packed, len = {0} bytes\n", byteLen);

            // build the NetHub transmission packet
            int src = 0;
        	int dst = 0;
        	
        	// first word is the packet length (big-endian)
        	sendBuffer[dst++] = (byte)((byteLen >> 8) & 0xFF);
        	sendBuffer[dst++] = (byte)(byteLen & 0xFF);

            // then of course the packet itself
            int limit = Math.Min( byteLen + 2 , sendBuffer.Length );
            while (dst < limit)
            {
        		ushort w = packet[src++];
        		sendBuffer[dst++] = (byte)((w >> 8) & 0xFF);
        		sendBuffer[dst++] = (byte)(w & 0xFF);
        	}
        	
            // transmit it
        	try {
        		str.Write(sendBuffer, 0, dst);
        		str.Flush();
        	} catch (Exception) {
    			// ignored
    		}
        }

        /// <summary>
        /// Shuts down the encapsulation provider.
        /// </summary>
        void IPacketInterface.Shutdown() {
        	//(debug) Console.WriteLine("** Stopping nethub interface...");
        	if (this.stream != null) { this.stream.Close(); }
        	if (this.client != null) { this.client.Close(); }
        	this.stopReceiverThread();
        	this.receiver = null;
        	Console.WriteLine("** Nethub interface shut down");
        }
        
        private void stopReceiverThread() {
        	if (this.receiverThread != null) {
        		this.receiverThread.Interrupt();
        		this.receiverThread.Join();
        		this.receiverThread = null;
        	}
        }
        
        private void PacketReceiver() {
        	Byte[] data = new Byte[1024];
        	try {
        		while(true) {
        			NetworkStream str = this.stream;
		        	if (str == null) {
		        		return;
		        	}
        			
        			// wait for next packet from the nethub
        			int byteLen = this.getLenPrefix(str);

                    // read the packet content up to the buffer size
        			int pos = 0;
        			while(pos < byteLen && pos < data.Length) {
        				data[pos++] = this.getByte(str);
        			}

                    // swallow the exceeding bytes in the packet
        			while(pos < byteLen) {
        				pos++;
        				this.getByte(str);
        			}
        			
        			// check if it is for us (to reduce traffic on this machine), also get sending machine for logging
					ulong dstAddress = readAddress(data, 0);
					ulong srcAddress = readAddress(data, 6);
        			if (dstAddress == this.localAddress || dstAddress == this.broadcastAddress) {
	        			// pass it the the ethernet controller
	        			ReceivePacketDelegate rcvr = this.receiver;
	        			if (rcvr != null) {
	        				//(debug) Console.Write("** received packed from 0x{0:x12} , len = {1} bytes\n", srcAddress, byteLen);
	        				rcvr(new MemoryStream(data, 0, byteLen));
	        			}
        			} else {
                        //(debug) Console.Write("~~ skipped packet to 0x{0:x16} (not us) from 0x{1:x16} , len = {2} bytes\n", dstAddress, srcAddress, byteLen);
                    }
        		}
        	}
	        catch(ThreadInterruptedException)
	        {
	            // ignored
	        }
        }
        
        private int getLenPrefix(NetworkStream str) {
        	Int32 b1 = this.getByte(str) & 0xFF;
        	Int32 b2 = this.getByte(str) & 0xFF;
        	return (b1 << 8) | b2;
        	
        }
        
        private byte getByte(NetworkStream str) {
        	int value = -1;
        	try {
        		value = str.ReadByte();
        	} catch(Exception) {
        		// ignored
        	}
        	if (value < 0) {
        		throw new ThreadInterruptedException();
        	}
        	return (byte)value;
        }
        
        private ulong readAddress(byte[] data, int at) {
        	ulong addr = 0;
        	for (int i = 0; i < 6; i++) {
        		addr <<= 8;
        		addr |= (ulong)(data[at++] & 0xFF);
        	}
        	return addr;
        }
		
	}
}
