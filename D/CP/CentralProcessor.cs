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


using D.IOP;
using D.Logging;
using System;

namespace D.CP
{
    public enum TaskType
    {
        Emulator = 0,
        Display,
        Ethernet,
        Refresh,
        Disk,
        IOP,
        IOPcs,          // Used as an address register when reading/writing control store
        Kernel,
    }

    public enum ClickType
    {
        Ethernet0 = 0,
        Disk,
        IOP,
        Ethernet1,
        Display
    }

    /// <summary>
    /// IB state, corresponding to the value of iBPtr
    /// </summary>
    public enum IBState
    {
        Full = 2,
        Word = 3,
        Byte = 1,
        Empty = 0,
    }

    /// <summary>
    /// The Dandelion Central Processor, processor implementation.
    /// This partial class implements the logic for the microcode engine.
    /// </summary>
    public partial class CentralProcessor : IIOPDevice, IDMAInterface
    {
        public CentralProcessor(DSystem system)
        {
            Reset();

            _system = system;
        }

        public void Reset()
        {
            _currentTask = TaskType.Kernel;

            _cycle = 1;     // Start in C1
            _click = ClickType.Ethernet0;

            // On reset, we need the IOP to wake us up before we
            // can start running.
            _iopWait_ = true;

            //
            // Clear interrupt flags and errors
            //
            _mInt = false;
            _eKErr = 0;
            _emulatorErrorTrap = false;
            _emulatorErrorTrapClickCount = 0;

            Array.Clear(_microcode, 0, _microcode.Length);
            Array.Clear(_microcodeCache, 0, _microcodeCache.Length);
            Array.Clear(_tpc, 0, _tpc.Length);
            Array.Clear(_tc, 0, _tc.Length);
            Array.Clear(_wakeup, 0, _wakeup.Length);
            Array.Clear(_rh, 0, _rh.Length);
            Array.Clear(_u, 0, _u.Length);
            Array.Clear(_link, 0, _link.Length);
            Array.Clear(_ib, 0, _ib.Length);
            
            _tpcAddr = 0;
            _stackP = 0;
            _ibPtr = IBState.Empty;
            _ibFront = 0;
            _ibEmptyCancel = false;
            _pc16 = false;
            _niaModifier = 0;
            _niaModifierType = NiaModiferType.Normal;
            _marPageCrossBr = false;
            _altUAddr = false;

            _swTAddr = false;
            _iopAttn = false;
            _cpDmaMode = false;
            _cpDmaIn = false;
            _cpDmaComplete_ = false;
            _wakeMode0 = false;
            _wakeMode1 = false;
            _cpAttn = false;
            _emuWake = false;
            _cpOutIntReq_ = true;
            _cpInIntReq_ = true;
            _iopReq = false;
            _inLatched = false;
            _outLatched = false;

            _exitKernel = false;
        }

        public ulong[] MicrocodeRam
        {
            get { return _microcode; }
        }

        public byte[] RH
        {
            get { return _rh; }
        }

        public ushort[] U
        {
            get { return _u; }
        }

        public TaskType CurrentTask
        {
            get { return _currentTask; }
        }

        public int[] TPC
        {
            get { return _tpc; }
        }

        public int NIAModifier
        {
            get { return _niaModifier; }
        }

        public AM2901 ALU
        {
            get { return _alu; }
        }

        public int Cycle
        {
            get { return _cycle; }
        }

        public int StackP
        {
            get { return _stackP; }
        }

        public byte IBFront
        {
            get { return _ibFront; }
        }

        public byte[] IB
        {
            get { return _ib; }
        }

        public IBState IBPtr
        {
            get { return _ibPtr; }
        }

        public bool PC16
        {
            get { return _pc16; }
        }

        /// <summary>
        /// Whether the processor is waiting to be awoken by the IOP.
        /// </summary>
        public bool IOPWait
        {
            get { return _iopWait_; }
        }

        /// <summary>
        /// Used by debuggers to allow stepping macrocode
        /// </summary>
        public bool IBDispatch
        {
            get
            {
                bool value = _ibDispatch;
                _ibDispatch = false;
                return value;
            }
        }       

        /// <summary>
        /// Executes the specified number of microinstructions
        /// </summary>
        public void ExecuteInstruction(int cycles)
        {
            for (int c = 0; c < cycles; c++)
            {
                //
                // Let the scheduler run for one clock.
                //
                _system.Scheduler.Clock();

                if (_iopWait_)
                {
                    //  Still waiting for the IOP to wake us.
                    continue;
                }

                //
                // TODO: General:
                //  Section 2.3.8 of the Dandelion Hardware manual outlines a set of timing restrictions
                //  for Xbus-related operations.  These restrictions are closely related to the mechanisms
                //  of the CP hardware and are tricky to emulate without actually simulating things at close
                //  to the component-level.  In particular, some Xbus operations are only valid across a subset
                //  of ALU bits (due to propagation delays between the AM2901s).  However: a naive emulation 
                //  should likely work for existing microcode since it would be written to meet these timing 
                //  restrictions -- said emulation would always return a valid result across all bits but this
                //  satisfies the microcode.  Where this falls down is that microcode that would be incorrect
                //  on a real Dandelion might "work" on the emulator.
                //  At this time, the naive implementation is Good Enough (tm) and once that's working, making
                //  a more technically correct implementation can be investigated.
                //

                //
                // Grab the next instruction from the cache.
                //            
                Microinstruction instruction = _microcodeCache[_tpc[(int)_currentTask]];

                bool cIn = instruction.Cin;
                bool invertPc16 = false;

                //
                // If the last instruction caused a PageCross branch during a memory operation, we must
                // cancel any MDR<-, IBDisp, or AlwaysIBDisp in this instruction.
                //
                bool pageCrossCancel = _marPageCrossBr;
                _marPageCrossBr = false;

                //
                // Latch the NIA modifier bits from the last instruction
                //
                int niaModifier = _niaModifier;
                _niaModifier = 0;

                //
                // Latch the AltUAddr bit from the last instruction
                //
                bool altUAddr = _altUAddr;
                _altUAddr = false;

                //
                // Save the previous instruction's Y Bus for AltUAddr.
                //
                ushort lastYBus = _yBus;

                //
                // Latch the IBError cancel bit (cancels an MDR<- operation if an IB read
                // in c1 failed due to an empty IB).
                //
                bool ibEmptyCancel = _ibEmptyCancel;
                _ibEmptyCancel = false;

                //
                // Latch the dispatch type from the last instruction so that we can
                // properly modify INIA at the end of this one.
                //
                NiaModiferType niaModifierType = _niaModifierType;
                _niaModifierType = NiaModiferType.Normal;

                //
                // Decode XBus sources:
                //

                //
                // Byte and/or Nibble constants from the microinstruction fields.
                // NB: instruction.Byte is set to 0 if this instruction does not specify a Byte or Nibble constant.
                //
                _xBus = instruction.Byte;

                switch (instruction.fSfZ)
                {
                    case FunctionSelectFZ.fzNorm:
                        switch ((ZNormFunction)instruction.fZ)
                        {
                            case ZNormFunction.LoadIBPtr1:
                                if (instruction.AlwaysIBDisp ||
                                    instruction.LoadIB)
                                {
                                    // This instruction uses IBPtr<-1 as a modifier; leave ibPtr/ibFront alone here; they will get modified later.
                                }
                                else
                                {
                                    if (_ibPtr != IBState.Byte)
                                    {
                                        _ibPtr = IBState.Byte;
                                        _ibFront = _ib[1];
                                    }

                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "ibPtr<-1: ibPtr={0}, ibFront=0x{1:x2}", _ibPtr, _ibFront);
                                }
                                break;

                            case ZNormFunction.LoadIBPtr0:
                                if (_ibPtr != IBState.Word)
                                {
                                    _ibPtr = IBState.Word;
                                    _ibFront = _ib[0];
                                }
                                if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "ibPtr<-0: ibPtr={0}, ibFront=0x{1:x2}", _ibPtr, _ibFront);
                                break;

                            case ZNormFunction.LoadCinFrompc16:
                                //
                                // Fun fact (from microcode ref, pg. 21):
                                // "Due to the way Cin is implemented in the hardware, when the Cin field of the microinstruction is
                                //  0, the fX version of Cin<-pc16 must be used.  (If the fZ version is used, Cin will be 0 instead
                                //  of pc16.)  If Cin=1, then either version of Cin<-pc16 can be used.
                                //
                                if (cIn)
                                {
                                    cIn = _pc16;
                                }
                                invertPc16 = true;   // Invert pc16 at the end of the cycle.
                                break;

                            case ZNormFunction.LoadBank:
                                throw new NotImplementedException("Bank<- not implemented.");

                            case ZNormFunction.AltUaddr:
                                _altUAddr = true;
                                break;

                            case ZNormFunction.LRot0:
                                if (instruction.ABypass)
                                {
                                    _xBus = _alu.R[instruction.rA];
                                }
                                break;

                            case ZNormFunction.LRot12:
                                if (instruction.ABypass)
                                {
                                    _xBus = (ushort)((_alu.R[instruction.rA] << 12) | (_alu.R[instruction.rA] >> 4));
                                }
                                break;

                            case ZNormFunction.LRot8:
                                if (instruction.ABypass)
                                {
                                    _xBus = (ushort)((_alu.R[instruction.rA] << 8) | (_alu.R[instruction.rA] >> 8));
                                }
                                break;

                            case ZNormFunction.LRot4:
                                if (instruction.ABypass)
                                {
                                    _xBus = (ushort)((_alu.R[instruction.rA] << 4) | (_alu.R[instruction.rA] >> 12));
                                }
                                break;
                        }
                        break;

                    case FunctionSelectFZ.IOXIn:
                        switch ((ZIOXIn)instruction.fZ)
                        {
                            case ZIOXIn.ReadEIdata:
                                _xBus = _system.EthernetController.EIData(_cycle);
                                break;

                            case ZIOXIn.ReadEStatus:
                                _xBus = _system.EthernetController.EStatus();
                                break;

                            case ZIOXIn.ReadKIData:
                                _xBus = _system.ShugartController.ReadKIData();
                                break;

                            case ZIOXIn.ReadKStatus:
                                _xBus = _system.ShugartController.ReadKStatus();
                                break;

                            case ZIOXIn.KStrobe:
                                _system.ShugartController.KStrobe();
                                break;

                            case ZIOXIn.ReadMStatus:
                                _xBus = _system.MemoryController.MStatus;
                                break;

                            case ZIOXIn.ReadKTest:
                                _xBus = _system.ShugartController.ReadKTest();
                                break;

                            case ZIOXIn.EStrobe:
                                _system.EthernetController.EStrobe(_cycle);
                                break;

                            case ZIOXIn.ReadIOPIData:
                                _xBus = ReadIOPData();
                                if (Log.Enabled) Log.Write(LogComponent.CPControl, "<-IOPData {0:x2}", _xBus);
                                break;

                            case ZIOXIn.ReadIOPStatus:
                                _xBus = ReadIOPStatus();
                                if (Log.Enabled) Log.Write(LogComponent.CPControl, "<-IOPStatus {0} ({1:x2})", (IOPStatusFlags)_xBus, _xBus);
                                break;

                            case ZIOXIn.ReadErrnIBnStkp:
                                // uCode reference, p.33:
                                // "X[8-9] = EKerr, X[10-11] = ~ibPtr, X[12-15] = ~stackP
                                //
                                _xBus =
                                    (ushort)((_eKErr << 6) |
                                    ((~(int)_ibPtr & 0x3) << 4) |
                                    (~_stackP & 0xf));
                                break;

                            case ZIOXIn.ReadRH:
                                _xBus = _rh[instruction.rB];
                                break;

                            case ZIOXIn.ReadibNA:
                                if (_ibPtr == IBState.Empty)
                                {
                                    // IB is empty, trap.
                                    SignalErrorTrap(ErrorTrap.IBEmpty);

                                    // Cancel MDR<- in c2
                                    _ibEmptyCancel = _cycle == 1;
                                }
                                else
                                {
                                    // xBus <- ibFront, leave ibPtr alone.
                                    _xBus = _ibFront;
                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "<-ibNA 0x{0:x2} ibPtr={1}", _ibFront, _ibPtr);
                                }
                                break;

                            case ZIOXIn.Readib:
                                if (_ibPtr == IBState.Empty)
                                {
                                    // IB is empty, trap.
                                    SignalErrorTrap(ErrorTrap.IBEmpty);

                                    // Cancel MDR<- in c2
                                    _ibEmptyCancel = _cycle == 1;
                                }
                                else
                                {
                                    // xBus <- ibFront, decrement ibPtr.
                                    _xBus = _ibFront;
                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "<-ib 0x{0:x2} ibPtr={1}", _ibFront, _ibPtr);

                                    _ibFront = _ib[((int)_ibPtr) & 0x1];
                                    DecrementIBPtr();

                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "ibFront now 0x{0:x2} ibPtr={1}", _ibFront, _ibPtr);
                                }
                                break;

                            case ZIOXIn.ReadibLow:
                                if (_ibPtr == IBState.Empty)
                                {
                                    // IB is empty, trap.
                                    SignalErrorTrap(ErrorTrap.IBEmpty);

                                    // Cancel MDR<- in c2
                                    _ibEmptyCancel = _cycle == 1;
                                }
                                else
                                {
                                    // low nibble of ibFront, leave ibPtr alone.
                                    _xBus = (ushort)(_ibFront & 0xf);

                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "<-ibLow 0x{0:x2} ibPtr={1}", _xBus, _ibPtr);
                                }
                                break;

                            case ZIOXIn.ReadibHigh:
                                if (_ibPtr == IBState.Empty)
                                {
                                    // IB is empty, trap.
                                    SignalErrorTrap(ErrorTrap.IBEmpty);

                                    // Cancel MDR<- in c2
                                    _ibEmptyCancel = _cycle == 1;
                                }
                                else
                                {
                                    // high nibble of ibFront, leave ibPtr alone.
                                    _xBus = (ushort)(_ibFront >> 4);

                                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "<-ibHigh 0x{0:x2} ibPtr={1}", _xBus, _ibPtr);
                                }
                                break;
                        }
                        break;
                }

                if (instruction.fX == XFunction.LoadCinFrompc16)
                {
                    cIn = _pc16;
                    invertPc16 = true;   // Invert pc16 at the end of the cycle.
                }

                if (instruction.SURead)
                {
                    // SU read operations
                    switch ((int)instruction.fSfZ)
                    {
                        case 0:
                        case 1:
                            _xBus = _u[_stackP];
                            break;

                        case 2:
                        case 3:
                            if (altUAddr)
                            {
                                // U address is rA,,Y[12-15] 
                                // (Y bus from *previous* instruction.)
                                _xBus = _u[(instruction.rA << 4) | (lastYBus & 0xf)];
                            }
                            else
                            {
                                // U address is rA,,fZ
                                _xBus = _u[instruction.UAddress];
                            }
                            break;
                    }
                }

                // Handle <-MD instructions which occur only in C3.
                if (instruction.mem && _cycle == 3)
                {
                    bool valid = false;
                    _xBus = _system.MemoryController.ReadMD(_currentTask, out valid);

                    if (!valid)
                    {
                        //
                        // Read from non-existent or uncorrectable memory.  This causes a double-bit memory fault.
                        //
                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPError, "Invalid memory access, address 0x{0:x5}", _system.MemoryController.MAR);
                        SignalErrorTrap(ErrorTrap.EmulatorMemoryError);
                    }
                }

                //
                // Generate new Y bus value after feeding it through the ALU.
                //
                _yBus = _alu.Execute(instruction, _xBus, cIn, (instruction.mem && _cycle == 1));

                //
                // Handle MAR<-, Map<- and MDR<-, which take their input from the Y Bus (+ YH for MAR<-).
                //
                if (instruction.MarMapMDR)
                {
                    switch (_cycle)
                    {
                        case 1:

                            if (instruction.LoadMap)
                            {
                                //
                                // Map<- : This is 0x10000 + YH[0-7],,Y[0-7], providing a 24-bit virtual address indexing
                                // into the virtual memory map (64k).
                                // Earlier CP boards only supported 22-bit VA's (using only YH[2-7], indexing into a 16k map.)
                                //
#if TWENTYTWOBITVA
                            int mapAddr = 0x10000 + ((((_rh[instruction.rB] & 0x3f) << 16) | _yBus) >> 8);
                            _system.MemoryController.LoadMAR(mapAddr);

                            if ((_rh[instruction.rB] & 0xc0) != 0)
                            {
                                SignalErrorTrap(ErrorTrap.EmulatorMemoryError);
                            }
#else
                                int mapAddr = 0x10000 + (((_rh[instruction.rB] << 16) | _yBus) >> 8);
                                _system.MemoryController.LoadMAR(mapAddr);
#endif

                                if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPMap, "Map<- 0x{0:x8} address loaded.", mapAddr);
                            }
                            else
                            {
                                //
                                // MAR<-: This is YH[4..7],,Y, providing a 20-bit physical address.
                                // Earlier CP boards only supported 18-bit physical addresses.
                                //
#if EIGHTEENBITPA
                            _system.MemoryController.LoadMAR(((_rh[instruction.rB] & 0x3) << 16) | _yBus);
#else
                                _system.MemoryController.LoadMAR(((_rh[instruction.rB] & 0xf) << 16) | _yBus);
#endif
                            }

                            //
                            // Do MAR<- side-effects: pageCross branch
                            // See the HW ref, page 25.
                            // pageCross is the XOR of the carry out from the low byte of the ALU with af[2].
                            //
                            if (instruction.mem && (_alu.PgCarry ^ (((int)instruction.aF & 0x1) == 1)))
                            {
                                // MAR<- enables a pageCross branch in INIA[10].
                                _niaModifier |= 0x2;
                                _marPageCrossBr = true;
                            }
                            break;

                        case 2:
                            //
                            // Sanity check that Map<- is not occurring in c2:
                            //
                            if (instruction.LoadMap)
                            {
                                throw new InvalidOperationException("Map<- in c2");
                            }

                            // MDR<-
                            if (!pageCrossCancel && !ibEmptyCancel)
                            {
                                _system.MemoryController.LoadMDR(_yBus);
                            }
                            break;

                        case 3:
                            //
                            // Sanity check that Map<- is not occurring in c3:
                            //
                            if (instruction.LoadMap)
                            {
                                throw new InvalidOperationException("Map<- in c3");
                            }
                            break;
                    }
                }

                //
                // Late LRotn functions.
                // These take the ALU output on the Y Bus and apply the specified rotation,
                // putting the result on the X Bus.
                //
                if (instruction.LateLRotN)
                {
                    switch ((ZNormFunction)instruction.fZ)
                    {
                        case ZNormFunction.LRot0:
                            _xBus = _yBus;
                            break;

                        case ZNormFunction.LRot12:
                            _xBus = (ushort)((_yBus << 12) | (_yBus >> 4));
                            break;

                        case ZNormFunction.LRot8:
                            _xBus = (ushort)((_yBus << 8) | (_yBus >> 8));
                            break;

                        case ZNormFunction.LRot4:
                            _xBus = (ushort)((_yBus << 4) | (_yBus >> 12));
                            break;
                    }
                }

                //
                // Load RH register from X Bus.
                //
                if (instruction.fX == XFunction.LoadRH)
                {
                    _rh[instruction.rB] = (byte)_xBus;
                    if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPExecution, "RH[0x{0:x2}] = 0x{1:x2}", instruction.rB, _xBus);
                }

                // Handle fY functions that aren't implicitly handled elsewhere (cycle, Byte, etc.)
                switch (instruction.fSfY)
                {
                    case FunctionSelectFY.fyNorm:
                        switch ((YNormFunction)instruction.fY)
                        {
                            case YNormFunction.ExitKern:
                                //
                                // Microcode ref, p 34 (section K.3):
                                // "When executed in c1, ExitKern will cause normal task scheduling to begin.  Thus,
                                //  which task runs in the click following ExitKern depends on where in the round structure
                                //  the ExitKern occured. [sic]"
                                //
                                if (_cycle == 1)
                                {
                                    _exitKernel = true;
                                }
                                break;

                            case YNormFunction.EnterKern:
                                throw new NotImplementedException("EnterKern not implemented.");

                            case YNormFunction.ClrIntErr:
                                //
                                // Clear pending interrupts and error state:
                                //
                                // HwRef sec 2.5.3:
                                // "MInt is ... cleared with fY = ClrIntErr.  (ClrIntErr also resets
                                //  the EKErr register.)"
                                //
                                // HwRef sec 2.5.5.2:
                                //  "[EKErr is] Cleared by ClrIntErr, which, as a side-effect, also resets any pending interrupts."
                                //
                                _mInt = false;
                                _eKErr = 0;
                                break;

                            case YNormFunction.IBDisp:
                                //
                                // NB: AlwaysIBDisp is not a special function, it is an assembler macro for IBDisp, IBPtr<- 1.
                                // This is canceled if the last memory operation resulted in a page cross.
                                if (!pageCrossCancel)
                                {
                                    if ((_ibPtr != IBState.Full || _mInt) && !instruction.AlwaysIBDisp)
                                    {
                                        //
                                        // From hw ref (2.5.5.1)
                                        // If an IBDisp is executed and ibPtr != full, the dispatch does not occur and instead a microcode trap is invoked.
                                        // The jump to the trap location occurs at the end of the next cycle (unlike emulator error traps which happen 
                                        // in a future c1).
                                        // - INIA[0-3] is replaced with 4 when ibPtr = empty or 5 when ibPtr != empty
                                        // - If there is a pending Mesa interrupt request (_mInt = 1):
                                        //    - INIA[0-3] is replaced with 6 if ibPtr = empty or full, or 7 otherwise.
                                        // Regardless, ibPtr does not change.
                                        //
                                        // A non-trapping IBDispatch is forced by fZ = IBPtr<-1 (i.e. AlwaysIBDisp).
                                        //
                                        if (_mInt)
                                        {
                                            _niaModifier |= (_ibPtr == IBState.Empty || _ibPtr == IBState.Full) ? 0x600 : 0x700;
                                            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "Mint trap to 0x{0:x3}", _niaModifier);

                                        }
                                        else
                                        {
                                            _niaModifier |= (_ibPtr == IBState.Empty) ? 0x400 : 0x500;
                                            if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "IB refill trap to 0x{0:x3}", _niaModifier);
                                        }

                                        _niaModifierType = NiaModiferType.IBRefillTrap;
                                    }
                                    else
                                    {
                                        //
                                        // Do the normal dispatch, and decrement ibPtr when done.
                                        // Unlike other dispatches, ibFront replaces some bits (rather than just or'ing) of
                                        // the next instruction's INIA.  This needs to be specially handled.
                                        // "The high 4 bits of ibFront replace INIA[4-7] while the low 4 bits of ibFront are OR'd
                                        // with INIA[8-11] (thereby allowing simultaneous branch/dispatches).  INIA[0-3] is unaffected.
                                        //
                                        _niaModifier |= _ibFront;
                                        _niaModifierType = NiaModiferType.IBDispatch;

                                        /*
                                        MacroInstruction inst = MacroInstruction.GetInstruction(MacroType.Lisp, _ibFront);

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPInst,
                                            "PC 0x{0:x5} -  0x{1:x2} ({2}) ibPtr ({3}) {4}",
                                            ((((_rh[5] & 0xf) << 16) | _alu.R[5]) << 1) | (_pc16 ? 1 : 0),
                                            _ibFront,
                                            inst.Mnemonic,
                                            inst.Operand,
                                            _ibPtr);
                                        */

                                        // new ibFront is IB[ibPtr[1]]
                                        _ibFront = _ib[((int)_ibPtr) & 0x1];

                                        DecrementIBPtr();

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "<-ibFront 0x{0:x2} ibPtr={1}", _ibFront, _ibPtr);
                                    }
                                }
                                break;

                            case YNormFunction.MesaIntRq:
                                _mInt = true;
                                break;

                            case YNormFunction.LoadIB:
                                if (instruction.LoadIBPtr1)
                                {
                                    //
                                    // If buffer is empty, the low byte goes to ibFront and the high
                                    // byte is discarded, otherwise load ib[0] and ib[1] and leave
                                    // ibFront alone.  (hwref, p. 20)
                                    //
                                    if (_ibPtr != IBState.Empty)
                                    {
                                        _ib[0] = (byte)(_xBus >> 8);
                                        _ib[1] = (byte)_xBus;

                                        // new ibPtr: "if empty THEN byte ELSE full"
                                        _ibPtr = IBState.Full;

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "ibPtr<-1 IB<-, notempty: ibPtr={0}, ib[0]=0x{1:x2} ib[1]=0x{2:x2}", _ibPtr, _ib[0], _ib[1]);
                                    }
                                    else
                                    {
                                        // new ibPtr: "if empty THEN byte ELSE full"
                                        _ibPtr = IBState.Byte;

                                        // new ibFront: "if ibPtr = empty THEN X[8-15] ELSE unchanged"
                                        _ibFront = (byte)_xBus;

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "ibPtr<-1 IB<-, empty: ibPtr={0}, ibFront=0x{1:x2}", _ibPtr, _ibFront);
                                    }
                                }
                                else
                                {
                                    // low byte to ib[1]
                                    _ib[1] = (byte)_xBus;

                                    if (_ibPtr != IBState.Empty)
                                    {
                                        // high byte to ib[0]
                                        _ib[0] = (byte)(_xBus >> 8);

                                        // new ibPtr: "if empty THEN word ELSE full"
                                        _ibPtr = IBState.Full;

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "IB<-, notempty: ibPtr={0}, ib[0]=0x{1:x2} ib[1]=0x{2:x2}", _ibPtr, _ib[0], _ib[1]);
                                    }
                                    else
                                    {
                                        // new ibFront: "if ibPtr = empty THEN X[0-7] ELSE unchanged"
                                        _ibFront = (byte)(_xBus >> 8);

                                        // new ibPtr: "if empty THEN word ELSE full"
                                        _ibPtr = IBState.Word;

                                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPIB, "IB<-, empty: ibPtr={0}, ibFront=0x{1:x2}, ib[0]=0x{2:x2} ib[1]=0x{3:x2}", _ibPtr, _ibFront, _ib[0], _ib[1]);
                                    }
                                }
                                break;

                            case YNormFunction.ClrDPRq:
                                _system.DisplayController.ClrDpRq();
                                break;

                            case YNormFunction.ClrIOPRq:
                                SleepTask(TaskType.IOP);
                                break;

                            case YNormFunction.ClrRefRq:
                                SleepTask(TaskType.Refresh);
                                break;

                            case YNormFunction.ClrKFlags:
                                _system.ShugartController.ClrKFlags();
                                break;
                        }
                        break;

                    case FunctionSelectFY.DispBr:
                        switch ((YDispBrFunction)instruction.fY)
                        {
                            case YDispBrFunction.NegBr:
                                if (_alu.Neg)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.ZeroBr:
                                if (_alu.Zero)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.NZeroBr:
                                if (!_alu.Zero)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.MesaIntBr:
                                if (_mInt)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.PgCarryBr:
                                if (_alu.PgCarry)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.CarryBr:
                                if (_alu.CarryOut)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.XRefBr:
                                //
                                // Dispatch on X[11] into INIA[11]
                                //
                                _niaModifier |= (_xBus & 0x10) >> 4;
                                break;

                            case YDispBrFunction.NibCarryBr:
                                if (_alu.NibCarry)
                                {
                                    _niaModifier |= 1;
                                }
                                break;

                            case YDispBrFunction.XDisp:
                                _niaModifier |= (_xBus & 0xf);
                                break;

                            case YDispBrFunction.YDisp:
                                _niaModifier |= (_yBus & 0xf);
                                break;

                            case YDispBrFunction.XC2npcDisp:
                                //
                                // Dispatch on X[12-13],,cycle 2,,~pc16 into INIA[8-11]
                                //
                                _niaModifier |= (_xBus & 0xc) | (_cycle == 2 ? 0x2 : 0x0) | (_pc16 ? 0x0 : 0x1);
                                break;

                            case YDispBrFunction.YIODisp:
                                //
                                // Dispatch on Y[12-13],,bp[39],,bp[139] 
                                // Also known as EtherDisp.
                                //
                                _niaModifier |= (_yBus & 0xc) | (_system.EthernetController.EtherDisp());
                                break;

                            case YDispBrFunction.XwdDisp:
                                //
                                // Dispatch on X.9,,X.10 into INIA 10,,11
                                //
                                _niaModifier |= (_xBus & 0x60) >> 5;
                                break;

                            case YDispBrFunction.XHDisp:
                                //
                                // Dispatch on X.4,,X.0
                                //
                                _niaModifier |= ((_xBus & 0x8000) >> 15) | ((_xBus & 0x0800) >> 10);
                                break;

                            case YDispBrFunction.XLDisp:
                                //
                                // Dispatch on X.8,,X.15
                                //
                                _niaModifier |= (_xBus & 0x1) | ((_xBus & 0x80) >> 6);
                                break;

                            case YDispBrFunction.PgCrOvDisp:
                                //
                                // Dispatch on _pageCross,,ALU overflow bit into INIA 10,,11.                            
                                //
                                // See the HW ref, page 25.
                                // pageCross is the XOR of the carry out from the low byte of the ALU with af[2].            
                                //
                                _niaModifier |= (_alu.PgCarry ^ (((int)instruction.aF & 0x1) == 1) ? 0x2 : 0x0) | (_alu.Overflow ? 0x1 : 0x0);
                                break;
                        }
                        break;

                    case FunctionSelectFY.IOOut:
                        switch ((YIOOutFunction)instruction.fY)
                        {
                            case YIOOutFunction.IOPOData:
                                if (Log.Enabled) Log.Write(LogComponent.CPControl, "IOPOData<- {0:x2}", (byte)_xBus);
                                WriteIOPData((byte)_xBus);
                                break;

                            case YIOOutFunction.IOPCtl:
                                WriteIOPCtl((byte)_xBus);
                                break;

                            case YIOOutFunction.KOData:
                                _system.ShugartController.SetKOData(_xBus);
                                break;

                            case YIOOutFunction.KCtl:
                                _system.ShugartController.SetKCtl(_xBus);
                                break;

                            case YIOOutFunction.EOData:
                                _system.EthernetController.EOData(_xBus);
                                break;

                            case YIOOutFunction.EICtl:
                                _system.EthernetController.EICtl(_xBus);
                                break;

                            case YIOOutFunction.DCtlFifo:
                                _system.DisplayController.SetDCtlFifo(_yBus);
                                break;

                            case YIOOutFunction.DCtl:
                                _system.DisplayController.SetDCtl(_xBus);
                                break;

                            case YIOOutFunction.DBorder:
                                _system.DisplayController.SetDBorder(_yBus);
                                break;

                            case YIOOutFunction.PCtl:
                                // Assume this is for the LSEP controller; docs are thin.
                                if (Log.Enabled) Log.Write(LogType.Error, LogComponent.CPExecution, "PCtl<-0x{0}, unimplemented.", _xBus);

                                if ((_xBus & 0x1) != 0)
                                {
                                    // Per MoonCycle.mc,
                                    // This wakes the LSEP/Refresh task (task 3).
                                    WakeTask(TaskType.Refresh);
                                }
                                else
                                {
                                    SleepTask(TaskType.Refresh);
                                }
                                break;

                            case YIOOutFunction.MCtl:
                                _system.MemoryController.SetMCtl(_yBus);
                                break;

                            case YIOOutFunction.EOCtl:
                                _system.EthernetController.EOCtl(_xBus);
                                break;

                            case YIOOutFunction.KCmd:
                                _system.ShugartController.SetKCmd(_xBus);
                                break;

                            case YIOOutFunction.POData:
                                throw new NotImplementedException("POData not implemented.");
                                break;

                            case YIOOutFunction.Invalid0:
                            case YIOOutFunction.Invalid1:
                                // Just a no-op
                                break;
                        }
                        break;
                }

                // SU reg write
                if (instruction.SUWrite)
                {
                    switch ((int)instruction.fSfZ)
                    {
                        case 0:
                        case 1:
                            _u[_stackP] = _yBus;
                            break;

                        case 2:
                        case 3:
                            if (altUAddr)
                            {
                                // U address is rA,,Y[12-15] 
                                // (Y bus from *previous* instruction.)
                                _u[(instruction.rA << 4) | (lastYBus & 0xf)] = _yBus;
                            }
                            else
                            {
                                // U address is rA,,fZ
                                _u[instruction.UAddress] = _yBus;
                            }
                            break;
                    }
                }

                //
                // pc16 gets inverted at the end of the cycle if fX or fZ is Cin<-pc16.
                // (HW ref, section 2.3.7)
                //
                if (invertPc16)
                {
                    _pc16 = !_pc16;
                }

                //
                // Stack modifications occur at the end of the microinstruction, handle them here.
                //
                if (instruction.LoadStackP)
                {
                    _stackP = (_yBus & 0xf);
                    if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stackp loaded, now 0x{0:x}", _stackP);
                }

                //
                // Handle pushes, pops, and stack pointer tests performed by performing multiple
                // pops/pushes at the same time.
                //
                if (instruction.StackOperation)
                {
                    switch (instruction.StackTest)
                    {
                        case StackTestType.None:
                            // Normal stack behavior.
                            if (instruction.Push)
                            {
                                if (_stackP == 0xf)
                                {
                                    // Overflow
                                    if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack overflow, raising error trap.");
                                    SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                                }
                                
                                _stackP = (_stackP + 1) & 0xf;
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Push: Stack pointer is now 0x{0:x}", _stackP);
                            }
                            else if (instruction.DoublePop)
                            {
                                // Test for 2 word underflow.
                                if (_stackP < 2)
                                {
                                    // Underflow
                                    if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack underflow, raising error trap.");
                                    SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                                }

                                // Still only decrement stackP by 1.
                                _stackP = (_stackP - 1) & 0xf;
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Double Pop: Stack pointer is now 0x{0:x}", _stackP);
                            }
                            else if (instruction.Pop)
                            {
                                if (_stackP == 0)
                                {
                                    // Underflow
                                    if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack underflow, raising error trap.");
                                    SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                                }

                                _stackP = (_stackP - 1) & 0xf;
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Pop: Stack pointer is now 0x{0:x}", _stackP);
                            }
                            break;

                        case StackTestType.Underflow:
                            // Test if a pop would cause an underflow
                            if (_stackP == 0x0)
                            {
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack underflow test passed, raising error trap.");
                                SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                            }
                            break;

                        case StackTestType.Overflow:
                            // Test if a push would cause an overflow
                            if (_stackP == 0xf)
                            {
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack overflow test passed, raising error trap.");
                                SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                            }
                            break;

                        case StackTestType.Underflow2:
                            // Test if stackP is 0 or 1
                            if (_stackP < 2)
                            {
                                if (Log.Enabled) Log.Write(LogComponent.CPStack, "Stack underflow (2) test passed, raising error trap.");
                                SignalErrorTrap(ErrorTrap.StackOverUnderflow);
                            }
                            break;
                    }
                }

                //
                // Calculate NIA based on this instruction's INIA field and condition/dispatch bits from the last
                // instruction.
                //
                int nia = instruction.INIA;

                //
                // If an error trap is pending we cancel any pending IBRefill trap.
                //            
                if (_eKErr > 0 && niaModifierType == NiaModiferType.IBRefillTrap)
                {
                    niaModifierType = NiaModiferType.Normal;
                    niaModifier = 0;
                    if (Log.Enabled) Log.Write(LogComponent.CPError, "Error trap pending, IBRefill trap aborted.");
                }

                switch (niaModifierType)
                {
                    case NiaModiferType.Normal:
                        // Normal dispatch, just OR the bits in
                        nia |= niaModifier;
                        break;

                    case NiaModiferType.IBDispatch:
                        // IBDisp dispatch: bits [4-7] are replaced, bits [8-11] are or'd.
                        nia = (nia & 0xf0f) | niaModifier;

                        //
                        // Set the IBDispatch breakpoint flag so that debuggers
                        // can be notified of a new Mesa instruction.
                        //
                        _ibDispatch = true;
                        break;

                    case NiaModiferType.IBRefillTrap:
                        // Refill Trap: bits [0-3] are replaced.
                        nia = (nia & 0x0ff) | niaModifier;
                        break;
                }

                //
                // Dispatch to the calculated NIA.
                //
                _tpc[(int)_currentTask] = nia;

                //
                // OR link bits into the next instruction's NIA (or save them) as necessary.
                //
                if (instruction.LinkAddress != -1)
                {
                    // Modify the link register on a write (indicated by NIA[7] == 0)
                    if ((nia & 0x10) == 0)
                    {
                        //
                        // Save the low nibble only.
                        //
                        _link[instruction.LinkAddress] = nia & 0xf;

                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPExecution, "Link[{0}] = {1:x}", instruction.LinkAddress, _link[instruction.LinkAddress]);
                    }
                    // Modify the NIA based on the link register (NIA[7] == 1)
                    else
                    {
                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPExecution, "nia modifier ({0:x3}),  Link[{1}] = {2:x}", _niaModifier, instruction.LinkAddress, _link[instruction.LinkAddress]);
                        //
                        // Or the link register in.
                        //
                        _niaModifier |= _link[instruction.LinkAddress];
                        if (Log.Enabled) Log.Write(LogType.Verbose, LogComponent.CPExecution, "nia modifier now {0:x3}", nia);
                    }
                }

                _cycle++;

                if (_cycle > 3)
                {
                    _cycle = 1;
                    TaskSwitch();
                }
            }
        }

        public void WakeTask(TaskType task)
        {
            _wakeup[(int)task] = true;

            if (Log.Enabled) Log.Write(LogComponent.CPTask, "Task {0} set to wake.", task);
        }

        /// <summary>
        /// Yes I know that "sleep" is an intransitive verb.
        /// </summary>
        /// <param name="task"></param>
        public void SleepTask(TaskType task)
        {
            _wakeup[(int)task] = false;

            if (Log.Enabled) Log.Write(LogComponent.CPTask, "Task {0} set to sleep.", task);
        }      

        /// <summary>
        /// Invoked at the end of a click.  If a task switch is necessary the proper task is selected and switched to.
        /// </summary>
        private void TaskSwitch()
        {
            //
            // Move to the next click.  This happens even while executing the Kernel task, though it
            // causes no task switching in that case.
            //
            _click = (ClickType)(((int)_click + 1) % 5);

            // if (Log.Enabled) Log.Write(LogComponent.CPTask, "Switch to click {0}", _click);

            //
            // If we are executing in the Kernel task and we aren't being asked to leave via ExitKernel
            // then no task switching takes place.
            //
            if (_exitKernel || _currentTask != TaskType.Kernel)
            {
                if (_exitKernel)
                {
                    // Remove the Kernel task wakeup signal.
                    SleepTask(TaskType.Kernel);
                    _exitKernel = false;
                }

                switch (_click)
                {
                    case ClickType.Ethernet0:
                    case ClickType.Ethernet1:
                        DoTaskSwitch(TaskType.Ethernet);
                        break;

                    case ClickType.Disk:
                        DoTaskSwitch(TaskType.Disk);
                        break;

                    case ClickType.IOP:
                        DoTaskSwitch(TaskType.IOP);
                        break;

                    case ClickType.Display:
                        if (_system.DisplayController.DisplayOn)
                        {
                            DoTaskSwitch(TaskType.Display);
                        }
                        else
                        {
                            DoTaskSwitch(TaskType.Refresh);
                        }
                        break;
                }

                _exitKernel = false;
            }

            //
            // If an emulator error trap occurred during some prior click and we're in the emulator
            // task now, trap to location 0 when the click count reaches zero.
            //
            if (_emulatorErrorTrap && _currentTask == TaskType.Emulator)
            {
                _emulatorErrorTrapClickCount--;

                if (_emulatorErrorTrapClickCount == 0)
                {
                    _emulatorErrorTrap = false;
                    _tpc[(int)_currentTask] = 0;
                    if (Log.Enabled) Log.Write(LogComponent.CPError, "Taking trap to 0 for eKerr {0}", _eKErr);
                }
            }
        }

        private void DoTaskSwitch(TaskType newTask)
        {
            TaskType nextTask;
            //
            // If the Kernel has been awoken we switch to the Kernel task regardless of anything else.
            //
            if (WakeStatus(TaskType.Kernel))
            {
                nextTask = TaskType.Kernel;
                if (Log.Enabled) Log.Write(LogComponent.CPTask, "Waking Kernel task.", _click);
            }
            else
            {
                //
                // If there is a wakeup for the requested task then we switch to that task,
                // otherwise we default to the Emulator task.
                //
                nextTask = WakeStatus(newTask) ? newTask : TaskType.Emulator;
            }
            
            if (nextTask == _currentTask)
            {
                // Nothing changed, nothing to do, early return.
                // if (Log.Enabled) Log.Write(LogComponent.CPTask, "No task switch this click.");
                return;
            }

            //
            // Save the current task's NIA condition bits to the TC array.  Only the 4 low bits are saved.
            //
            _tc[(int)_currentTask] = _niaModifier & 0xf;

            //
            // Restore the new tasks's NIA condition bits
            //
            _niaModifier = _tc[(int)nextTask];

            //
            // Switch to the new task.
            //
            _currentTask = nextTask;
            
            if (Log.Enabled) Log.Write(LogComponent.CPTask, "Task switch to {0}", _currentTask);
        }        

        private bool WakeStatus(TaskType task)
        {
            return _wakeup[(int)task];
        }

        private void SignalErrorTrap(ErrorTrap err)
        {
            if (err == ErrorTrap.ControlStoreParity)
            {
                // Not implemented, probably will never be...
                throw new NotImplementedException("CS Parity errors not implemented.");
            }

            // Set the emulator-kernel error register.
            // TODO: Smaller values have priority over larger.
            if ((int)err < _eKErr || (_eKErr == 0 && !_emulatorErrorTrap))
            {
                _eKErr = (int)err;
            }

            //
            // An error trap transfers control of the Emulator task
            // to location 0; this will occur in c1 one or two emulator clicks in the future (including
            // the current click) depending on the trap and the cycle the trap occurred in.
            // The hardware requires the execution of one additional Emulator click before the trap.
            // (hwref, pg. 33).
            //
            if (!_emulatorErrorTrap)
            {
                _emulatorErrorTrap = true;
                switch (err)
                {
                    case ErrorTrap.ControlStoreParity:
                        //
                        // "If the instruction read from microstore in c1 has bad parity, then the Kernel
                        //  runs at location 0 in the next c1.  If the parity error occurs in c2 or c3, then there
                        //  is a one click delay before the Kernel executes at location 0 in c1.
                        //
                        _emulatorErrorTrapClickCount = _cycle == 1 ? 1 : 2;
                        break;

                    case ErrorTrap.EmulatorMemoryError:
                        // "The hardware requires the execution of one additional Emulator click between the c3 which 
                        //  errored and the trap at location 0."
                        _emulatorErrorTrapClickCount = 2;
                        break;

                    case ErrorTrap.StackOverUnderflow:
                        // "The hardware requires the execution of on additional Emulator click before the trap at location 0."
                        _emulatorErrorTrapClickCount = 2;
                        break;

                    case ErrorTrap.IBEmpty:
                        // "If the IB-Empty error occurs in c1, then control transfers to location 0 in the next Emulator c1.
                        //  However if the error occurs in c2 or c3, the hardware requires the execution of one additional
                        //  Emulator click before the trap at location 0."
                        _emulatorErrorTrapClickCount = _cycle == 1 ? 1 : 2;
                        break;
                }                
                if (Log.Enabled) Log.Write(LogComponent.CPError, "Error trap {0} at address {1:x3}.  Jumping to CS address 0 for task {2} in future c1.", err, _tpc[(int)_currentTask], _currentTask);
            }                       
        }

        /// <summary>
        /// Does the unique "decrement" of ibPtr -- 2, 3, 1, 0 (full, word, byte, empty)
        /// </summary>
        private void DecrementIBPtr()
        {
            _ibPtr = _nextIBPtr[(int)_ibPtr];
        }

        private enum ErrorTrap
        {
            ControlStoreParity = 0,
            EmulatorMemoryError = 1,
            StackOverUnderflow = 2,
            IBEmpty = 3,
        }

        private enum NiaModiferType
        {
            Normal,
            IBDispatch,
            IBRefillTrap,
        }

        // Task/Temporary Program Counters
        private int[] _tpc = new int[8];

        // Task/Temporary Condition bits (NIA modifiers).  This is only 4 bits.
        private int[] _tc = new int[8];

        // Task wakeups
        private bool[] _wakeup = new bool[8];

        // Current task
        private TaskType _currentTask;

        // Microcode store
        private ulong[] _microcode = new ulong[4096];

        // Microcode decode cache
        private Microinstruction[] _microcodeCache = new Microinstruction[4096];

        // 2901 ALU
        private AM2901 _alu = new AM2901();

        // RH registers, 8 bit
        private byte[] _rh = new byte[16];

        // Link registers, 4 bit
        // NOTE: Link register:
        // See section 2.5.4 of the HW ref;
        // Link is addressed by fX and is written with the low nibble of NIAX when
        // fX is in 0..7 and NIA[7] = 0;
        // A Link register is or'd into the low nibble of INIA when fX is in 0..7 and
        // NIA[7] = 1.  If the preceding uinstruction does not specify a branch/dispatch,
        // the Link register is loaded with a constant.
        // However if the prior instruction does specify branch/dispatch, the value loaded
        // depends on the outcome of the branch or dispatch.
        private int[] _link = new int[8];

        // U registers
        private ushort[] _u = new ushort[256];

        // Instruction buffer (IB)
        private byte _ibFront;
        private byte[] _ib = new byte[2];
        private IBState _ibPtr;        
        private bool _ibEmptyCancel;

        // Table of values for next ipPtr value when decrementing ibPtr.
        private readonly IBState[] _nextIBPtr = { IBState.Empty, IBState.Empty, IBState.Word, IBState.Byte };

        // Stack pointer, 4 bits
        private int _stackP;

        // pc16 register, 1 bit
        private bool _pc16;

        // Bus data
        private ushort _xBus;
        private ushort _yBus;

        // NIA modifier for branch/dispatch
        private int _niaModifier;
        private NiaModiferType _niaModifierType;

        // AltUAddress flag
        private bool _altUAddr;

        //
        // Interrupt flags
        //
        private bool _mInt;

        //
        // Error state
        //

        //
        // HWRef, section 2.5.5.2:
        // The EKErr register, read onto X[8-9] with <-ErrnIBnStkp, names the type of error:
        //   0 - control store parity error
        //   1 - Emulator memory error
        //   2 - stackPointer overflow or underflow
        //   3 - IB-Empty error
        // If, coincidentally, two or more error occur at the same time, smaller values of EKErr
        // are reported.  The error types are also accumulated until EKErr is reset: the minimum
        // value is reported when EKErr is read.
        // Cleared by ClrIntErr, which, as a side-effect, also resets any pending interrupts.
        private int _eKErr;
        private bool _emulatorErrorTrap;
        private int _emulatorErrorTrapClickCount;

        /// <summary>
        /// Whether a PageCross branch occurred during the last MAR<- operation.
        /// Cleared at the beginning of the next instruction, and used to indicate whether
        /// an MDR<-, IBDisp, or AlwaysIBDisp should be canceled.
        /// </summary>
        private bool _marPageCrossBr;

        //
        // Cycle / Click / Round data
        //
        private int _cycle;                 // c1 ... c3
        private ClickType _click;           // 0 ... 4

        //
        // Whether to exit the Kernel task at the end of this click
        //
        private bool _exitKernel;

        //
        // Debugging flag: Indicates that an IBDispatch has occurred,
        // allows handling Mesa (or other bytecode) instruction breakpoints.
        //
        private bool _ibDispatch;

        //
        // The D System we belong to
        //
        private DSystem _system;
    }
}
