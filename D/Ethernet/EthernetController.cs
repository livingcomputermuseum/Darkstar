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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace D.Ethernet
{
    /// <summary>
    /// EthernetController implements the Star's Ethernet controller.
    /// At this time, no official documentation exists; only microcode listings and the schematic drawings.
    /// The code is implemented based on study of the schematics and diagnostic microcode.
    /// As such there is a lot of handwavy stuff in here, especially around loopback.  At this time enough
    /// is implemented to get boot diagnostics to pass.
    /// 
    /// Questions remaining to be answered:
    /// - What is the distinction between Loopback and LocalLoopback?  Initially it looked like LocalLoopback only
    ///   looped back through the FIFO, but it appears to invoke CRC generation and microcode comments make it look
    ///   like transmission actually takes place?  At this time both are treated identically.
    /// - How is the FIFO actually controlled during loopback?  (How does the transmit hardware know when to stop
    ///   transmitting when the FIFO is (apparently) being used for both transmit and receive at the same time during
    ///   a loopback operation?)    
    ///   
    /// - Why is CRC calculation not working the way I expect?  The residual sum does not appear to work out to
    ///   the expected value for Ethernet CRC32. 
    ///  
    /// </summary>
    public class EthernetController
    {
        public EthernetController(DSystem system)
        {
            _system = system;
            _fifo = new Queue<ushort>();
            _inputPacket = new Queue<ushort>();
            _outputPacket = new Queue<ushort>();
            _pendingPackets = new Queue<MemoryStream>();

            _crc32 = new CRC32();

            // Attach real Ethernet device if user has specified one, otherwise leave unattached; output data
            // will go into a bit-bucket.
            try
            {
                
                if (Configuration.HostRawEthernetInterfacesAvailable &&
                    !string.IsNullOrWhiteSpace(Configuration.HostPacketInterfaceName))
                { 
                    _hostInterface = new HostEthernetEncapsulation(Configuration.HostPacketInterfaceName);
                    _hostInterface.RegisterReceiveCallback(OnHostPacketReceived);
                }
            }
            catch (Exception e)
            {
                _hostInterface = null;
                Log.Write(LogComponent.HostEthernet, "Unable to configure network interface.  Error {0}", e.Message);
            }

            _readerLock = new ReaderWriterLockSlim();

            // Start the ethernet reciever poll event, this will run forever.            
            _system.Scheduler.Schedule(_receiverPollInterval, ReceiverPollCallback);

            Reset();
        }

        public void Reset()
        {
            _turnOff_ = false;
            _rxEvenLen = false;
            _rxGoodCRC = false;
            _rxOverrun_ = true;
            _txUnderrun = false;
            _txCollision_ = true;
            _rxMode_ = true;
            _enableTx = false;
            _lastWord = false;
            _enableRcv = false;
            _localLoop = false;
            _loopBack = false;
            _defer = false;

            _purge = false;
            _tickElapsed = false;
            _outAttn = false;
            _inAttn = false;
            _transmitterRunning = false;

            _outputData = 0;
            _outputDataLatched = false;
            _fifo.Clear();
            _inputPacket.Clear();
            _crc32.Reset();
        }

        public int EtherDisp()
        {
            //
            // Pin 139(YIODisp.1) : Hooked to "Attn," which appear to be whether any attention is needed by the receiver 
            //                      or transmitter.
            // Pin 39(YIODisp.0) : "(schematic) Must be zero for the transmitting inner loop uCode.  It is also used to 
            //                      determine if the Option card is plugged in."            
            //
            int value = 0;

            if (_turnOff_)
            {
                //
                // Ethernet is not turned off, returned value is based on whether
                // the transmitter or reciever hardware has a status to report.                
                value = _outAttn | _inAttn ? 1 : 0;
            }

            return value;
        }        

        public ushort EStatus()
        {
            ushort value = (ushort)
                   ~((_turnOff_ ? 0x0001 : 0x00) |
                   (_rxEvenLen ? 0x0002 : 0x00) |
                   (_rxGoodCRC ? 0x0004 : 0x00) |
                   (_rxOverrun_ ? 0x0008 : 0x00) |
                   (_rxGoodAlign ? 0x0010 : 0x00) |
                   (!_txUnderrun ? 0x0020 : 0x00) |
                   (_txCollision_ ? 0x0040 : 0x00) |
                   (_rxMode_ ? 0x0080 : 0x00) |
                   (_enableTx ? 0x0100 : 0x00) |
                   (_lastWord ? 0x0200 : 0x00) |
                   (_enableRcv ? 0x0400 : 0x00) |
                   (_localLoop ? 0x0800 : 0x00) |
                   (_loopBack ? 0x1000 : 0x00));

            return value;
        }             

        public void EOCtl(ushort value)
        {
            // EOCtl:             Bit(etc)
            // ----------------------------
            // EnableTrn          15
            // LastWord           14
            // Defer              13
            _enableTx = (value & 0x1) != 0;
            _lastWord = (value & 0x2) != 0;
            _defer = (value & 0x4) != 0;

            _outAttn = false;
            _txUnderrun = false;
            _txCollision_ = true;

            if (Log.Enabled) Log.Write(LogComponent.EthernetControl,
                "EOCtl<- 0x{0:x4}: enableTx {1} lastWord {2} defer {3}",
                value,
                _enableTx,
                _lastWord,
                _defer
                );

            //
            // Writing EOCtl resets the defer clock
            //
            _tickElapsed = false;
            _system.Scheduler.Cancel(_deferEvent);

            if (_defer)
            {
                //
                // Queue up an event 51.2uS in the future, this will
                // set _tickElapsed, update wakeups, and start the transmitter when it fires.
                //
                _deferEvent = _system.Scheduler.Schedule(_deferDelay, DeferCallback);
            }
            else
            {
                //
                // Start the transmitter running if need be (if it isn't already).
                //
                if (_enableTx && !_transmitterRunning && !_lastWord)
                {
                    _crc32.Reset();
                    StartTransmitter();
                }
            }

            if (!_enableTx)
            {
                _fifo.Clear();
                StopTransmitter();
            }

            UpdateWakeup();

            if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EOCtl end.");
        }

        public void EICtl(ushort value)
        {
            // EICtl:             Bit(xerox order)
            // ------------------------------------
            // EnableRcv          15
            // TurnOff'           14
            // LocalLoop          13
            // LoopBack           12
            _enableRcv = (value & 0x1) != 0;
            _turnOff_ = (value & 0x2) == 0;
            _localLoop = (value & 0x4) != 0;
            _loopBack = (value & 0x8) != 0;

            if (!_enableRcv)
            {
                // Reset receive state
                _rxMode_ = true;
                _receiverState = ReceiverState.Preamble;
                _inAttn = false;

                _rxGoodCRC = true;
                _rxGoodAlign = true;
                _rxOverrun_ = true;
                _rxEvenLen = true;

                if (!_loopBack)
                {
                    _fifo.Clear();
                }

                _inputPacket.Clear();
                _crc32.Reset();
            }

            UpdateWakeup();

            if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EICtl<- 0x{0:x4} enablerx {1} turnOff' {2} localLoop {3} loopBack {4}.",
                value,
                _enableRcv,
                _turnOff_,
                _localLoop,
                _loopBack);
        }

        public void EOData(ushort value)
        {
            if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EOData<- 0x{0:x4}.", value);
            
            _outputData = value;
            _outputDataLatched = true;
        }

        public void EStrobe(int cycle)
        {
            if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EStrobe.");

            if ((cycle == 1 || cycle == 3) & !_lastWord)
            {
                // Strobe output data into FIFO.

                if (!_outputDataLatched)
                {
                    // This is not actually an underrun case, it indicates a case where the microcode is doing
                    // something we do not expect.
                    if (Log.Enabled) Log.Write(LogType.Error, LogComponent.EthernetControl, "EStrobe: no data latched.");
                    _outAttn = false;
                }

                //
                // Move data from output data word into the FIFO.
                //
                if (_fifo.Count < 16)
                {
                    _fifo.Enqueue(_outputData);
                    _outputDataLatched = false;

                    if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EStrobe: loaded word 0x{0:x4} into FIFO.  FIFO count is now {1}",
                        _outputData, _fifo.Count);

                    _outAttn = false;

                    UpdateWakeup();
                }
                else
                {
                    _fifo.Dequeue();
                    _fifo.Enqueue(_outputData);
                    // This should not happen; microcode should sleep when the FIFO is full.
                    if (Log.Enabled) Log.Write(LogType.Error, LogComponent.EthernetControl, "EStrobe: FIFO full, dropping word.");                    
                }
            }
            else if (cycle == 2)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "EStrobe: (cycle 2) flushing received data.");

                // Throw out input data and stop the receiver
                _fifo.Clear();
                _inputPacket.Clear();
                _inAttn = false;
                _rxMode_ = true;
                StopReceiver();
                UpdateWakeup();
            }
        }

        public ushort EIData(int cycle)
        {
            ushort value = 0;
            
            //
            // Read from the input/output FIFO.
            //
            if (_fifo.Count > 0)
            {
                value = _fifo.Dequeue();
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "<-EIData: Returning FIFO word 0x{0:x4}.  FIFO count is now {1}",
                    value, _fifo.Count);
            }
            else
            {
                if (Log.Enabled) Log.Write(LogType.Error, LogComponent.EthernetControl, "<-EIData: FIFO empty.");

                // TODO: does this cause an underrun?
            }

            return value;
        }

        private void DeferCallback(ulong skewNsec, object context)
        {
            _tickElapsed = true;
            UpdateWakeup();
            _tickElapsed = false;

            //
            // Start the transmitter.
            //
            if (_enableTx && !_transmitterRunning)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Defer complete, starting transmitter.");
                StartTransmitter();
            }
        }

        private void StartTransmitter()
        {
            //
            // Start transmit clock running; this will wake up every
            // 1600ns to pick up a word (if available) from the fifo
            // and transmit it.  (If Defer is active, we will do this
            // after the deferral period has elapsed.)
            //
            if (!_transmitterRunning)
            {
                //
                // First abort any transmit clock that may be running.
                //
                _system.Scheduler.Cancel(_transmitEvent);

                //
                // Schedule the transmission callback.
                _transmitEvent = _system.Scheduler.Schedule(_ipgInterval, TransmitCallback);

                //
                // Clear the output packet.
                //
                _outputPacket.Clear();

                _transmitterRunning = true;
            }
            else
            {
                throw new InvalidOperationException("Transmitter already running.");
            }
        }

        private void StopTransmitter()
        {
            _system.Scheduler.Cancel(_transmitEvent);
            _transmitterRunning = false;
        }

        private void TransmitWord(ushort word)
        {
            if (_localLoop || _loopBack)
            {
                // Loop back to FIFO through the receiver.

                //
                // Append this word to the input packet.
                // It will be picked up by the Receive callback and
                // put into the FIFO in due time.
                //                
                _inputPacket.Enqueue(word);

                //
                // Ensure the receiver is running.
                //
                RunReceiver();
            }
            else
            {
                // Append to outgoing packet.
                _outputPacket.Enqueue(word);
            }
        }

        private void CompleteTransmission()
        {
            //
            // Transmit completed packet over real ethernet.
            //            

            //
            // A properly formed packet generated by the microcode should begin with the standard ethernet 
            // SFD of 3 words of 0x5555 and 1 word of 0x55d5.  This must be stripped before we send it
            // to the host device.
            //
            if (_outputPacket.Count < 4)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Malformed packet: too short.");
                return;
            }

            bool badSfd = false;
            for(int i=0;i<4;i++)
            {
                ushort sfdWord = _outputPacket.Dequeue();

                if (i < 3)
                {
                    badSfd = sfdWord != 0x5555;
                }
                else
                {
                    badSfd = sfdWord != 0x55d5;
                }
            }

            if (badSfd)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Malformed packet: Invalid SFD.");
                return;
            }

            if (_outputPacket.Count > 0 && _hostInterface != null)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Transmitting completed packet.");
                _hostInterface.Send(_outputPacket.ToArray());
            }
        }

        private void TransmitCallback(ulong skewNsec, object context)
        {
            //
            // Pull the next word from the FIFO, if available.
            //            
            if (_fifo.Count > 0)
            {
                ushort nextWord = _fifo.Dequeue();
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Transmitting word 0x{0:x4}", nextWord);
                TransmitWord(nextWord);
            }
            else if (!_lastWord)
            {
                //
                // No data available in FIFO and LastWord is not set: Underrun.  
                // Raise txUnderrun to signal an error.
                //
                _txUnderrun = true;
                if (Log.Enabled) Log.Write(LogType.Error, LogComponent.EthernetControl, "Transmit underrun.");
            }            
            
            if (_lastWord && _fifo.Count == 0)
            {
                //
                // If LastWord is set and the FIFO is empty, that will be the last word in the packet.  Shut things down.
                //
                _transmitterRunning = false;
                _outAttn = true;
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Last word.  Stopping transmission.");                

                //
                // Transmit completed packet over real ethernet.
                //
                CompleteTransmission();
            }
            else if (_txUnderrun)
            {                
                _transmitterRunning = false;
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Underrun.  Stopping transmission.");
            }
            else
            {
                //
                // Still going, schedule the next callback.
                //
                _transmitEvent = _system.Scheduler.Schedule(_transmitInterval, TransmitCallback);
            }

            //
            // Update wakeups -- if the FIFO has space now, the microcode should be awakened, for example.
            //
            UpdateWakeup();
        }

        /// <summary>
        /// Invoked when the host ethernet interface receives a packet destined for us.
        /// NOTE: This runs on the PCap or UDP receiver thread, not the main emulator thread.
        ///       Any access to emulator structures must be properly protected.
        /// 
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void OnHostPacketReceived(MemoryStream data)
        {
            //
            // Append the new packet onto our pending packets queue.
            // This will be picked up when the receiver is ready to receive things.
            //
            _readerLock.EnterUpgradeableReadLock();
            if (!_enableRcv || !_turnOff_)
            {
                //
                // Receiver is off, just drop the packet on the floor.                
                //
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Ethernet receiver is off; dropping this packet.");

                _readerLock.EnterWriteLock();
                    _pendingPackets.Clear();
                _readerLock.ExitWriteLock();
            }
            else if (_pendingPackets.Count < 32)
            {
                //
                // Place the packet into the queue; this will be picked up by the receiver poll thread
                // and passed to the receiver.
                //
                _readerLock.EnterWriteLock();
                    _pendingPackets.Enqueue(data);
                _readerLock.ExitWriteLock();

                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Packet (length {0}) added to pending buffer.", data.Length);
            }
            else
            {
                //
                // Too many queued-up packets, drop this one.
                //
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Pending buffer full; dropping this packet.");
            }
            _readerLock.ExitUpgradeableReadLock();
        }

        private void StopReceiver()
        {
            _system.Scheduler.Cancel(_receiveEvent);
            _receiverRunning = false;
        }

        private void RunReceiver()
        {
            _rxMode_ = false;

            if (!_receiverRunning && _enableRcv)
            {
                //
                // This is a hack: For loopback cases the real hardware has mysterious state machines to deal with
                // tracking the FIFO properly (since it's being used for both input and output at the same time,
                // something that only occurs during loopback testing -- the complication is how the transmit state machine knows
                // when the last word provided by the microcode has been sent, when the loopback is bringing new words into the FIFO
                // at the same time).
                // Because we live in a fantasy world of emulation, we can cheat: To keep things simple here we simply delay the 
                // receive operation to ensure that there is no overlap between the transmit and receive on loopback, this avoids
                // needing extra logic for the FIFO during loopback tests.
                //
                _receiveEvent = _system.Scheduler.Schedule(
                    _localLoop || _loopBack ? _receiveIntervalLoopback : _receiveInterval,
                    ReceiveCallback);
                
                _receiverRunning = true;
            }
        }

        private void ReceiverPollCallback(ulong skewNsec, object context)
        {
            if (!_enableRcv || !_turnOff_ || _enableTx || _transmitterRunning || _localLoop || _loopBack)
            {
                //
                // Receiver is off, we're currently transmitting, or we're in loopback mode, we do nothing.
                // 
            }
            else
            {
                //
                // See if there's a packet to pick up.
                //
                MemoryStream packetStream = null;

                _readerLock.EnterWriteLock();
                if (!_receiverRunning && _pendingPackets.Count > 0)
                {
                    // We have a packet, dequeue it and dump it into the receiver input queue.
                    packetStream = _pendingPackets.Dequeue();
                }
                _readerLock.ExitWriteLock();

                if (packetStream != null)
                {
                    //
                    // Read the stream into the receiver input queue.
                    //
                    packetStream.Seek(0, SeekOrigin.Begin);

                    while (packetStream.Position < packetStream.Length)
                    {
                        _inputPacket.Enqueue((ushort)((packetStream.ReadByte() << 8) | packetStream.ReadByte()));
                    }

                    //
                    // Skip the preamble state (only used in loopback)
                    //
                    _receiverState = ReceiverState.Data;

                    //
                    // Alert the microcode to the presence of input data and start processing.
                    //
                    RunReceiver();

                    if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Receive: Incoming packet queued into input buffer.");
                }
            }
            
            //
            // Schedule the next poll callback.
            //            
            _system.Scheduler.Schedule(_receiverPollInterval, ReceiverPollCallback);
        }

        private void ReceiveCallback(ulong skewNsec, object context)
        {
            //
            // Pull the next word from the input packet and run the state machine.
            //
            if (_inputPacket.Count > 0)
            {
                ushort nextWord = _inputPacket.Dequeue();

                switch (_receiverState)
                {
                    case ReceiverState.Preamble:
                        if (nextWord == 0x55d5) // end of preamble
                        {
                            if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Receive: end of preamble, switching to Data state.");
                            _receiverState = ReceiverState.Data;
                        }
                        break;

                    case ReceiverState.Data:
                        //
                        // Stuff into FIFO.
                        //
                        if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Receive: Enqueuing Data word 0x{0:x4} onto FIFO, {1} words left.", nextWord, _inputPacket.Count);
                        _fifo.Enqueue(nextWord);
                        _crc32.AddToChecksum(nextWord);
                        UpdateWakeup();
                        if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Packet CRC is now 0x{0:x8}", _crc32.Checksum);
                        break;
                }
            }

            if (_inputPacket.Count > 0)
            {
                //
                // Post next event if there are still words left.
                //                
                _receiveEvent = _system.Scheduler.Schedule(_transmitInterval, ReceiveCallback);
            }
            else
            {             
                //
                // End of packet.
                //
                _receiverRunning = false;

                //
                // Let microcode know the packet is done.
                //
                _inAttn = true;

                //
                // Update CRC and other flags.
                //
                _rxMode_ = false;

                _rxGoodCRC = _loopBack || _localLoop ? _crc32.Checksum == _goodCRC : true;

                UpdateWakeup();

                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Final Packet CRC is 0x{0:x8}", _crc32.Checksum);
            }
        }

        private void UpdateWakeup()
        {
            //
            // See schematic, pg 2; ethernet requests (wakeups) generated by:            // TxMode & BufIR & Defer' & LastWord' (i.e.transmit on, fifo buffer not full, not deferring, not the last word)            //     OR            // Defer & TickElapsed (microcode asked for the transmission to be deferred, and that deferral time has elapsed)            //     OR            // RcvMode & BufOR & Purge' (i.e. rcv on, fifo data ready, not purging fifo)            //    OR            // Attn (i.e.hardware has a a status to report)
            //
            bool txWakeup = _enableTx && _fifo.Count < 16 && !_defer && !_lastWord;
            bool deferWakeup = _defer & _tickElapsed;
            bool rxWakeup = !_rxMode_ && _fifo.Count > 2 && !_purge;

            if (txWakeup || deferWakeup || rxWakeup || _outAttn || _inAttn)
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Waking Ethernet task (tx {0} defer {1} rx {2} outAttn {3} inAttn {4}", txWakeup, deferWakeup, rxWakeup, _outAttn, _inAttn);
                _system.CP.WakeTask(TaskType.Ethernet);
            }
            else
            {
                if (Log.Enabled) Log.Write(LogComponent.EthernetControl, "Sleeping Ethernet task.");
                _system.CP.SleepTask(TaskType.Ethernet);
            }
        }        

        private DSystem _system;

        // Output data
        private bool _outputDataLatched;
        private ushort _outputData;
        private Queue<ushort> _fifo;

        // Input data
        private Queue<ushort> _inputPacket;
        private Queue<MemoryStream> _pendingPackets;

        // Defer timings
        private bool _tickElapsed;

        // Attention flags
        private bool _outAttn;
        private bool _inAttn;

        private bool _purge;

        // Status Bit(in Xerox order)
        // --------------------------------
        // TurnOff'        :  15
        // R.EvenLen       :  14
        // R.GoodCRC       :  13
        // R.Overrun'      :  12
        // R.GoodAlign     :  11
        // T.Underrun'     :  10
        // T.Collision'    :  9
        // RcvMode'        :  8
        // EnableTrn       :  7
        // LastWord        :  6
        // EnableRcv       :  5
        // LocalLoop       :  4
        // Loopback        :  3

        // DiagVideoData   :  2
        // VideoClock      :  1      // Used by LSEP
        // DiagLineSync    :  0
        private bool _turnOff_;        private bool _rxEvenLen;        private bool _rxGoodCRC;        private bool _rxOverrun_;        private bool _rxGoodAlign;        private bool _txUnderrun;        private bool _txCollision_;        private bool _rxMode_;        private bool _enableTx;        private bool _lastWord;        private bool _enableRcv;        private bool _localLoop;        private bool _loopBack;

        // EOCtl:             Bit(etc)
        // ----------------------------
        // EnableTrn          15
        // LastWord           14
        // Defer              13
        private bool _defer;


        // EICtl:             Bit(xerox order)        // ------------------------------------        // EnableRcv          15        // TurnOff'           14        // LocalLoop          13        // LoopBack           12
        // (See above)

        //
        // Defer event & timing -- 51.2uS
        //
        private Event _deferEvent;
        private readonly ulong _deferDelay = (ulong)(51.2 * Conversion.UsecToNsec);

        //
        // Transmit event and timing -- 1600nS
        //        
        private readonly ulong _transmitInterval = 1200;
        private readonly ulong _ipgInterval = (ulong)(9.6 * Conversion.UsecToNsec);  // Inter-packet gap
        private bool _transmitterRunning;
        private Event _transmitEvent;

        //
        // Receive event, timing, and thread safety
        //        
        private readonly ulong _receiveInterval = 1200;
        private readonly ulong _receiveIntervalLoopback = 25600;
        private bool _receiverRunning;
        private ReceiverState _receiverState;
        private Event _receiveEvent;
        
        private readonly ulong _receiverPollInterval = (ulong)(51.2 * Conversion.UsecToNsec);

        private ReaderWriterLockSlim _readerLock;

        private enum ReceiverState
        {
            Off,
            Preamble,
            Data
        }

        //
        // CRC32 generator
        //
        private CRC32 _crc32;
        // private const uint _goodCRC = 0xc704dd7b;
        private const uint _goodCRC = 0x2144df1c;   // This is not the correct residual for Ethernet but it's what I'm getting right now...

        //
        // Host ethernet
        //
        private IPacketInterface _hostInterface;
        private Queue<ushort> _outputPacket;

    }
}
