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

namespace D.CP
{
    public enum AluSourcePair
    {
        AQ = 0,
        AB = 1,
        ZQ = 2,
        ZB = 3,
        ZA = 4,
        DA = 5,
        DQ = 6,
        D0 = 7,
    }

    public enum AluFunction
    {
        RplusS      = 0,
        SminusR     = 1,
        RminusS     = 2,
        RorS        = 3,
        RandS       = 4,
        notRandS    = 5,
        RxorS       = 6,
        notRxorS    = 7,
    }

    public enum FunctionSelectFY
    {
        DispBr = 0,
        fyNorm = 1,
        IOOut  = 2,
        Byte   = 3,
    }

    public enum FunctionSelectFZ
    {
        fzNorm = 0,
        Nibble = 1,
        Uaddr  = 2,
        IOXIn  = 3,
    }

    public enum XFunction
    {
        pCallRet0   = 0x0,
        pCallRet1   = 0x1,
        pCallRet2   = 0x2,
        pCallRet3   = 0x3,
        pCallRet4   = 0x4,
        pCallRet5   = 0x5,
        pCallRet6   = 0x6,
        pCallRet7   = 0x7,
        Noop        = 0x8,
        LoadRH      = 0x9,
        shift       = 0xa,
        cycle       = 0xb,
        LoadCinFrompc16 = 0xc,
        LoadMap     = 0xd,
        pop         = 0xe,
        push        = 0xf,
    }

    public enum YNormFunction
    {
        ExitKern    = 0x0,
        EnterKern   = 0x1,
        ClrIntErr   = 0x2,
        IBDisp      = 0x3,
        MesaIntRq   = 0x4,
        LoadstackP  = 0x5,
        LoadIB      = 0x6,
        cycle       = 0x7,
        Noop        = 0x8,
        LoadMap     = 0x9,
        Refresh     = 0xa,
        push        = 0xb,
        ClrDPRq     = 0xc,
        ClrIOPRq    = 0xd,
        ClrRefRq    = 0xe,
        ClrKFlags   = 0xf,
    }

    public enum YDispBrFunction
    {
        NegBr       = 0x0,
        ZeroBr      = 0x1,
        NZeroBr     = 0x2,
        MesaIntBr   = 0x3,
        PgCarryBr   = 0x4,
        CarryBr     = 0x5,
        XRefBr      = 0x6,
        NibCarryBr  = 0x7,
        XDisp       = 0x8,
        YDisp       = 0x9,
        XC2npcDisp  = 0xa,
        YIODisp     = 0xb,
        XwdDisp     = 0xc,
        XHDisp      = 0xd,
        XLDisp      = 0xe,          // AKA XDirtyDisp
        PgCrOvDisp  = 0xf,
    }

    public enum YIOOutFunction
    {
        IOPOData    = 0x0,
        IOPCtl      = 0x1,
        KOData      = 0x2,
        KCtl        = 0x3,
        EOData      = 0x4,
        EICtl       = 0x5,
        DCtlFifo    = 0x6,
        DCtl        = 0x7,
        DBorder     = 0x8,
        PCtl        = 0x9,
        MCtl        = 0xa,
        Invalid0    = 0xb,
        EOCtl       = 0xc,
        KCmd        = 0xd,
        Invalid1    = 0xe,
        POData      = 0xf,
    }

    public enum ZNormFunction
    {
        Refresh     = 0x0,
        LoadIBPtr1  = 0x1,
        LoadIBPtr0  = 0x2,
        LoadCinFrompc16 = 0x3,
        LoadBank    = 0x4,
        pop         = 0x5,
        push        = 0x6,
        AltUaddr    = 0x7,
        Noop0       = 0x8,
        Noop1       = 0x9,
        Noop2       = 0xa,
        Noop3       = 0xb,
        LRot0       = 0xc,
        LRot12      = 0xd,
        LRot8       = 0xe,
        LRot4       = 0xf
    }

    // For Zap Rowsdower
    public enum ZIOXIn
    {
        ReadEIdata  = 0x0,
        ReadEStatus = 0x1,
        ReadKIData  = 0x2,
        ReadKStatus = 0x3,
        KStrobe     = 0x4,
        ReadMStatus = 0x5,
        ReadKTest   = 0x6,
        EStrobe     = 0x7,
        ReadIOPIData = 0x8,
        ReadIOPStatus = 0x9,
        ReadErrnIBnStkp = 0xa,
        ReadRH      = 0xb,
        ReadibNA    = 0xc,
        Readib      = 0xd,
        ReadibLow   = 0xe,
        ReadibHigh  = 0xf,
    }

    public enum StackTestType
    {
        None,
        Underflow,
        Overflow,
        Underflow2,
    }

    /// <summary>
    /// Decodes a single microcode word.
    /// </summary>
    public class Microinstruction
    {
        public Microinstruction(ulong word)
        {
            rA =                (int)((word & 0xf00000000000) >> 44);
            rB =                (int)((word & 0x0f0000000000) >> 40);
            aS =      (AluSourcePair)((word & 0x00e000000000) >> 37);
            aF =        (AluFunction)((word & 0x001c00000000) >> 34);
            aD =                (int)((word & 0x000300000000) >> 32);
            ep =                      (word & 0x000080000000) != 0;
            Cin =                     (word & 0x000040000000) != 0;
            enSU =                    (word & 0x000020000000) != 0;
            mem =                     (word & 0x000010000000) != 0;
            fSfY = (FunctionSelectFY)((word & 0x00000c000000) >> 26);
            fSfZ = (FunctionSelectFZ)((word & 0x000003000000) >> 24);
            fX =          (XFunction)((word & 0x000000f00000) >> 20);
            fY =                (int)((word & 0x0000000f0000) >> 16);
            fZ =                (int)((word & 0x00000000f000) >> 12);
            INIA =              (int)((word & 0x000000000fff));


            //
            // Instruction metadata that can be precomputed and cached
            //
            Cycle = (fX == XFunction.cycle) ||
                    (fSfY == FunctionSelectFY.fyNorm && ((YNormFunction)fY) == YNormFunction.cycle);
            Shift =
                ((fX == XFunction.shift) ||
                 Cycle);

            AluNeedsXBus = (aS == AluSourcePair.D0 || aS == AluSourcePair.DA || aS == AluSourcePair.DQ);

            AluDestination = aD | (Shift ? 0x4 : 0x0);

            SURead = enSU && !Cin;

            SUWrite = enSU && Cin;

            LoadMap = fX == XFunction.LoadMap ||
                 (fSfY == FunctionSelectFY.fyNorm &&
                  (YNormFunction)fY == YNormFunction.LoadMap);

            ABypass = AluDestination == 0x2;

            LoadStackP = (fSfY == FunctionSelectFY.fyNorm &&
                         (YNormFunction)fY == YNormFunction.LoadstackP);

            LoadIBPtr1 = (fSfZ == FunctionSelectFZ.fzNorm && ((ZNormFunction)fZ) == ZNormFunction.LoadIBPtr1);

            AlwaysIBDisp = (fSfY == FunctionSelectFY.fyNorm &&
                            (YNormFunction)fY == YNormFunction.IBDisp) &&
                            LoadIBPtr1;

            LoadIB = fSfY == FunctionSelectFY.fyNorm &&
                      (YNormFunction)fY == YNormFunction.LoadIB;

            UAddress = (rA << 4) | fZ;

            switch (fX)
            {
                case XFunction.pCallRet0:
                case XFunction.pCallRet1:
                case XFunction.pCallRet2:
                case XFunction.pCallRet3:
                case XFunction.pCallRet4:
                case XFunction.pCallRet5:
                case XFunction.pCallRet6:
                case XFunction.pCallRet7:
                    LinkAddress = (int)fX;
                    break;

                default:
                    LinkAddress = -1;
                    break;
            }

            MarMapMDR = mem || LoadMap;

            LateLRotN = !ABypass && fSfZ == FunctionSelectFZ.fzNorm;

            if (fSfY == FunctionSelectFY.Byte)
            {
                // Byte constant
                Byte = (byte)((fY << 4) | fZ);
            }
            else if (fSfZ == FunctionSelectFZ.Nibble)
            {
                // Nibble constant
                Byte = (byte)fZ;
            }
            else
            {
                // No constant value.
                Byte = 0;
            }

            bool fxPop = (fX == XFunction.pop);
            bool fzPop = (fSfZ == FunctionSelectFZ.fzNorm && ((ZNormFunction)fZ) == ZNormFunction.pop);

            Pop = fxPop || fzPop;

            //
            // There is a special case if both fxPop and fzPop are specified: stackP is still decremented by 1,
            // but a trap is invoked if stackP is 1 or 0 (rather than just 0).
            //
            DoublePop = fxPop && fzPop;
                   
            Push = (fX == XFunction.push) ||
                   (fSfY == FunctionSelectFY.fyNorm && ((YNormFunction)fY) == YNormFunction.push) ||
                   (fSfZ == FunctionSelectFZ.fzNorm && ((ZNormFunction)fZ) == ZNormFunction.push);

            StackOperation = Pop || Push;

            //
            // From the HWref (p. 33):
            // "Multiple pop's and push's can be specified per microinstruction in order to ameliorate the detection
            //  of Stack overflow or underflow.  For instance, fXpop (i.e. the pop in the fX field), fZpop, and
            //  push executed together leave the stackPointer unmodified, yet simulate two pop's with respect to
            //  stack underflow detection..."
            // The actual overflow detection logic is controlled by a PROM, there's nothing too weird going on 
            // here (other than overloading to provide only semi-related semantics, which is annoying.)  At 
            // any rate, we precompute the check that's being requested (if any) so we don't have to do it 
            // at execution time.
            // TODO: might make sense to dump the PROM and use that.
            //
            if (fxPop && fzPop && Push)
            {
                StackTest = StackTestType.Underflow2;
            }
            else if (Push && fzPop)
            {
                StackTest = StackTestType.Overflow;
            }
            else if (fxPop && Push)
            {
                StackTest = StackTestType.Underflow;
            }
            else
            {
                // No non-modify test, just normal stack behavior.
                StackTest = StackTestType.None;
            }

        }

        /// <summary>
        /// 2901 A reg addr, U addr [0-3]
        /// </summary>
        public readonly int rA;

        /// <summary>
        /// 2901 B reg addr, RH addr
        /// </summary>
        public readonly int rB;

        /// <summary>
        /// 2901 alu Source operand pair
        /// </summary>
        public readonly AluSourcePair aS;

        /// <summary>
        /// 2901 alu Function
        /// </summary>
        public readonly AluFunction aF;

        /// <summary>
        /// 2901 alu Destination/shift control
        /// </summary>
        public readonly int aD;

        /// <summary>
        /// Even Parity
        /// </summary>
        public readonly bool ep;

        /// <summary>
        /// 2901 Carry In, Shift Ends, writeSU (if enSU = 1)
        /// </summary>
        public readonly bool Cin;

        /// <summary>
        /// enable SU reg file
        /// </summary>
        public readonly bool enSU;

        /// <summary>
        /// MAR<- (if c1), MDR<- (if c2), <-MD (if c3)
        /// </summary>
        public readonly bool mem;

        /// <summary>
        /// Function field selector for Y
        /// </summary>
        public readonly FunctionSelectFY fSfY;

        /// <summary>
        /// Function field selector for Z
        /// </summary>
        public readonly FunctionSelectFZ fSfZ;

        /// <summary>
        /// X Function
        /// </summary>
        public readonly XFunction fX;

        /// <summary>
        /// Y Function
        /// </summary>
        public readonly int fY;

        /// <summary>
        /// Z Function
        /// </summary>
        public readonly int fZ;

        /// <summary>
        /// Next Instruction Address
        /// </summary>
        public readonly int INIA;

        //
        // The following are metadata for this instruction, used to speed execution.
        //

        /// <summary>
        /// Instruction specifies a Cycle of ALU output when writing back to R/Q
        /// </summary>
        public readonly bool Cycle;

        /// <summary>
        /// Instruction specifies a Shift of ALU as above.
        /// </summary>
        public readonly bool Shift;

        /// <summary>
        /// Instruction requires XBus input to ALU.
        /// </summary>
        public readonly bool AluNeedsXBus;

        /// <summary>
        /// Destination control for the ALU
        /// </summary>
        public readonly int AluDestination;

        /// <summary>
        /// Instruction specifies an SU register read
        /// </summary>
        public readonly bool SURead;

        /// <summary>
        /// Instruction specifies an SU register write
        /// </summary>
        public readonly bool SUWrite;

        /// <summary>
        /// Instruction specifies a Map<- operation.
        /// </summary>
        public readonly bool LoadMap;

        /// <summary>
        /// Instruction uses the A-bypass mode for the ALU.
        /// </summary>
        public readonly bool ABypass;

        /// <summary>
        /// Instruction specifies a stackP<- operation.
        /// </summary>
        public readonly bool LoadStackP;

        /// <summary>
        /// Instruction specifies a push operation
        /// </summary>
        public readonly bool Push;

        /// <summary>
        /// Instruction specifies a pop operation
        /// </summary>
        public readonly bool Pop;

        /// <summary>
        /// Instruction specifies a double-pop operation.
        /// </summary>
        public readonly bool DoublePop;

        /// <summary>
        /// Whether any stack operations (pushes or pops) occur in this instruction.
        /// </summary>
        public readonly bool StackOperation;

        /// <summary>
        /// Specifies the kind of test specified by the various
        /// push/pop instruction fields.
        /// </summary>
        public readonly StackTestType StackTest;

        /// <summary>
        /// Causes an IBDisp branch even if IB is not full;
        /// specified by IBDisp + IBPtr<-1
        /// </summary>
        public readonly bool AlwaysIBDisp;

        /// <summary>
        /// Whether an IB<- is specified this instruction.
        /// </summary>
        public readonly bool LoadIB;

        /// <summary>
        /// Whether the instruction specifies an ibPtr<-1 operation,
        /// which can be used to modify other operations.
        /// </summary>
        public readonly bool LoadIBPtr1;

        /// <summary>
        /// Constant address used to address U register when loading/storing
        /// </summary>
        public readonly int UAddress;

        /// <summary>
        /// Constant byte value
        /// </summary>
        /// <returns></returns>
        public readonly byte Byte;

        /// <summary>
        /// Link address specified by instruction (or -1 if not specified)
        /// </summary>
        public readonly int LinkAddress;

        /// <summary>
        /// Whether the instruction specifies an MAR<-, Map<-, or MDR<- operation.
        /// </summary>
        public readonly bool MarMapMDR;

        /// <summary>
        /// Whether to do an LrotN operation after the ALU runs.
        /// </summary>
        public readonly bool LateLRotN;

        public override string ToString()
        {
            return String.Format("rA={0:x} rB={1:x} aS={2} aF={3} aD={4} ep={5} Cin={6} enSU={7} mem={8} fSY={9} fSZ={10} fX={11} fY={12:x} fZ={13:x} INIA={14:x3}",
                rA, rB, aS, aF, aD, ep, Cin, enSU, mem, fSfY, fSfZ, fX, fY, fZ, INIA);
        }

        public string Disassemble(int cycle)
        {
            //
            // Build ALU op, start with the sources:
            //

            string aluR;
            string aluS;
            bool Rzero = false;
            bool Szero = false;

            string xBusValue = DisassembleXBusSource(cycle);

            switch (aS)
            {
                case AluSourcePair.AB:
                    aluR = String.Format("R{0:x}", rA);
                    aluS = String.Format("R{0:x}", rB);
                    break;

                case AluSourcePair.AQ:
                    aluR = String.Format("R{0:x}", rA);
                    aluS = "Q";
                    break;

                case AluSourcePair.ZA:
                    aluR = "0";
                    Rzero = true;
                    aluS = String.Format("R{0:x}", rA);
                    break;

                case AluSourcePair.ZB:
                    aluR = "0";
                    Rzero = true;
                    aluS = String.Format("R{0:x}", rB);
                    break;

                case AluSourcePair.ZQ:
                    aluR = "0";
                    Rzero = true;
                    aluS = "Q";
                    break;

                case AluSourcePair.D0:
                    aluR = xBusValue;
                    aluS = "0";
                    Szero = true;
                    break;

                case AluSourcePair.DA:
                    aluR = xBusValue;
                    aluS = String.Format("R{0:x}", rA);
                    break;

                case AluSourcePair.DQ:
                    aluR = xBusValue;
                    aluS = "Q";
                    break;

                default:
                    throw new InvalidOperationException("Unexpected ALU source pair.");
            }

            //
            // Select operation
            //
            string aluOp;            
            switch (aF)
            {
                case AluFunction.RplusS:
                    if (Rzero)
                    {
                        aluOp = aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = aluR;
                    }
                    else
                    {
                        aluOp = String.Format("{0}+{1}", aluR, aluS);
                    }
                    break;

                case AluFunction.SminusR:
                    if (Rzero)
                    {
                        aluOp = aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = "-" + aluR;
                    }
                    else
                    {
                        aluOp = String.Format("{0}-{1}", aluS, aluR);
                    }
                    break;

                case AluFunction.RminusS:
                    if (Rzero)
                    {
                        aluOp = "-" + aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = aluR;
                    }
                    else
                    {
                        aluOp = String.Format("{0}-{1}", aluR, aluS);
                    }
                    break;

                case AluFunction.RorS:
                    if (Rzero)
                    {
                        aluOp = aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = aluR;
                    }
                    else
                    {
                        aluOp = String.Format("{0} or {1}", aluR, aluS);
                    }
                    break;

                case AluFunction.RandS:
                    if (Rzero)
                    {
                        aluOp = "0";
                    }
                    else if (Szero)
                    {
                        aluOp = "0";
                    }
                    else
                    {
                        aluOp = String.Format("{0} and {1}", aluR, aluS);
                    }
                    break;

                case AluFunction.notRandS:
                    if (Rzero)
                    {
                        aluOp = aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = "0";
                    }
                    else
                    {
                        aluOp = String.Format("~{0} and {1}", aluR, aluS);
                    }
                    break;

                case AluFunction.RxorS:
                    if (Rzero)
                    {
                        aluOp = aluS;
                    }
                    else if (Szero)
                    {
                        aluOp = aluR;
                    }
                    else
                    {
                        aluOp = String.Format("{0} xor {1}", aluR, aluS);
                    }
                    break;

                case AluFunction.notRxorS:
                    if (Szero)
                    {
                        aluOp = "~" + aluR;
                    }
                    else
                    {
                        aluOp = String.Format("~{0} xor {1}", aluR, aluS);
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unexpected ALU operation");
            }

            //
            // Select register writeback (to rB)
            // Q writeback, and Y source (F, or A bypass)
            //
            int writeFn = aD | (Shift ? 0x4 : 0x0);
            string regAssignment;
            bool aBypass = false;
            bool yBusIsSourceForDestination = false;
            bool aluNoWriteBack = false;
            switch(writeFn)
            {
                case 0:
                    // no write, Q<-F
                    regAssignment = String.Format("Q<- {0}{1}", aluOp, GetCarryMod());
                    yBusIsSourceForDestination = true;
                    break;

                case 1:
                    // no write.
                    regAssignment = String.Format("{0}{1}", aluOp, GetCarryMod()); 
                    yBusIsSourceForDestination = false;
                    aluNoWriteBack = true;
                    break;

                case 2:                    
                    // R[rB] <- F, no write to Q, A Bypass for YBus<-
                    regAssignment = String.Format("R{0:x}<- {1}{2}", rB, aluOp, GetCarryMod());
                    aBypass = true;
                    yBusIsSourceForDestination = true;
                    break;

                case 3:
                    // R[rB] <- F, no write to Q
                    regAssignment = String.Format("R{0:x}<- {1}{2}", rB, aluOp, GetCarryMod());
                    yBusIsSourceForDestination = true;
                    break;

                case 4:
                    if (Cycle)
                    {
                        // double-word right shift
                        regAssignment = String.Format("R{0:x}<- DRShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    else
                    {
                        // double-word arithmetic right shift.
                        regAssignment = String.Format("R{0:x}<- DARShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    yBusIsSourceForDestination = true;
                    break;

                case 5:
                    if (Cycle)
                    {
                        // F: single-word right rotate:
                        regAssignment = String.Format("R{0:x}<- RRot1 {1}{2}", rB, aluOp, GetCarryMod());
                    }
                    else
                    {
                        // F: single-word right shift w/carryIn to MSB:
                        regAssignment = String.Format("R{0:x}<- RShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    yBusIsSourceForDestination = true;
                    break;

                case 6:                    
                    if (Cycle)
                    {
                        // double-word left shift
                        regAssignment = String.Format("R{0:x}<- DLShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    else
                    {
                        // double-word arithmetic left shift
                        regAssignment = String.Format("R{0:x}<- DALShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    yBusIsSourceForDestination = true;
                    break;

                case 7:
                    if (Cycle)
                    {
                        // single-word left rotate:
                        regAssignment = String.Format("R{0:x}<- LRot1 {1}{2}", rB, aluOp, GetCarryMod());
                    }
                    else
                    {
                        // single-word left shift w/carryIn to MSB:
                        regAssignment = String.Format("R{0:x}<- LShift1 {1}{2}{3}", rB, aluOp, GetCarryMod(), Cin ? " SE<-1" : String.Empty);
                    }
                    yBusIsSourceForDestination = true;
                    break;

                default:
                    throw new InvalidOperationException("Unexpected sh,,aD value.");
            }

            string yBusValue = aBypass ? String.Format("R{0:x}, {1}", rA, regAssignment) : String.Format("{0}", regAssignment);

            string fxFunc = String.Empty;
            bool xBusIsSourceForDestination = false;

            bool yBusBranch = false;
            bool xBusBranch = false;

            // Handle fX functions that aren't implicitly handled elsewhere (shift, cycle)
            switch (fX)
            {
                case XFunction.pCallRet0:
                case XFunction.pCallRet1:
                case XFunction.pCallRet2:
                case XFunction.pCallRet3:
                case XFunction.pCallRet4:
                case XFunction.pCallRet5:
                case XFunction.pCallRet6:
                case XFunction.pCallRet7:
                    fxFunc = String.Format("pCall/Ret{0} ", (int)fX);
                    break;

                case XFunction.LoadRH:
                    fxFunc = String.Format("RH{0:x}<-", rB);
                    xBusIsSourceForDestination = true;
                    break;

                case XFunction.LoadCinFrompc16:
                    fxFunc = "SE<-pc16 ";
                    break;

                case XFunction.LoadMap:
                    fxFunc = String.Format("Map<- RH{0:x},,", rB);
                    yBusIsSourceForDestination = true;
                    break;

                case XFunction.pop:
                    fxFunc = "pop ";
                    break;

                case XFunction.push:
                    fxFunc = "push ";
                    break;
            }

            string fyFunc = String.Empty;

            // Handle fY functions that aren't implicitly handled elsewhere (cycle, Byte, etc.)
            switch (fSfY)
            {
                case FunctionSelectFY.fyNorm:
                    switch ((YNormFunction)fY)
                    {
                        case YNormFunction.ExitKern:
                            fyFunc = "ExitKern ";
                            break;

                        case YNormFunction.EnterKern:
                            fyFunc = "EnterKern ";
                            break;

                        case YNormFunction.ClrIntErr:
                            fyFunc = "ClrIntErr ";
                            break;

                        case YNormFunction.IBDisp:
                            fyFunc = "IBDisp ";
                            break;

                        case YNormFunction.MesaIntRq:
                            fyFunc = "MesaIntRq ";
                            break;

                        case YNormFunction.LoadstackP:
                            fyFunc = "stackP<-";
                            yBusIsSourceForDestination = true;
                            break;

                        case YNormFunction.LoadIB:
                            fyFunc = "IB<-";
                            xBusIsSourceForDestination = true;
                            break;

                        case YNormFunction.LoadMap:
                            fyFunc = String.Format("Map<- RH{0:x},,", rB);
                            break;

                        case YNormFunction.Refresh:
                            fyFunc = "Refresh ";
                            break;

                        case YNormFunction.push:
                            fyFunc = "push ";
                            break;

                        case YNormFunction.ClrDPRq:
                            fyFunc = "ClrDPRq ";
                            break;

                        case YNormFunction.ClrIOPRq:
                            fyFunc = "ClrIOPRq ";
                            break;

                        case YNormFunction.ClrRefRq:
                            fyFunc = "ClrRefRq ";
                            break;

                        case YNormFunction.ClrKFlags:
                            fyFunc = "ClrKFlags ";
                            break;
                    }                    
                    break;

                case FunctionSelectFY.DispBr:
                    fyFunc = ((YDispBrFunction)fY).ToString() + " ";

                    switch ((YDispBrFunction)fY)
                    {
                        case YDispBrFunction.NegBr:
                        case YDispBrFunction.ZeroBr:
                        case YDispBrFunction.NibCarryBr:
                        case YDispBrFunction.PgCarryBr:
                        case YDispBrFunction.CarryBr:
                        case YDispBrFunction.PgCrOvDisp:
                        case YDispBrFunction.YDisp:
                        case YDispBrFunction.YIODisp:
                            yBusBranch = true;
                            break;

                        case YDispBrFunction.XRefBr:
                        case YDispBrFunction.XwdDisp:
                        case YDispBrFunction.XHDisp:
                        case YDispBrFunction.XLDisp:
                        case YDispBrFunction.XDisp:
                        case YDispBrFunction.XC2npcDisp:
                            xBusBranch = true;
                            break;
                    }

                    break;

                case FunctionSelectFY.IOOut:
                    if (fY != 0xb && fY != 0xe)
                    {
                        YIOOutFunction yIOOut = ((YIOOutFunction)fY);
                        fyFunc = yIOOut.ToString() + "<-";

                        // IOOut functions are roughly split between taking data from the XBus or the YBus.
                        xBusIsSourceForDestination =
                            (yIOOut == YIOOutFunction.IOPOData ||
                             yIOOut == YIOOutFunction.IOPCtl ||
                             yIOOut == YIOOutFunction.KOData ||
                             yIOOut == YIOOutFunction.KCtl ||
                             yIOOut == YIOOutFunction.EOData ||
                             yIOOut == YIOOutFunction.EICtl ||
                             yIOOut == YIOOutFunction.DCtl ||
                             yIOOut == YIOOutFunction.PCtl ||
                             yIOOut == YIOOutFunction.EOCtl ||
                             yIOOut == YIOOutFunction.KCmd ||
                             yIOOut == YIOOutFunction.POData
                            );

                        yBusIsSourceForDestination = (!xBusIsSourceForDestination && (fY != 0xb && fY != 0xe));
                        
                    }
                    break;
            }

            string fzFunc = String.Empty;

            // Handle fZ functions that aren't implicitly handled elsewhere (IOXIn)
            switch (fSfZ)
            {
                case FunctionSelectFZ.fzNorm:
                    switch((ZNormFunction)fZ)
                    {
                        case ZNormFunction.Refresh:
                            fzFunc = "Refresh ";
                            break;

                        case ZNormFunction.LoadIBPtr1:
                            fzFunc = "IBPtr<-1 ";
                            break;

                        case ZNormFunction.LoadIBPtr0:
                            fzFunc = "IBPtr<-0 ";
                            break;

                        case ZNormFunction.LoadCinFrompc16:
                            fzFunc = "SE<-pc16 ";
                            break;

                        case ZNormFunction.pop:
                            fzFunc = "pop ";
                            break;

                        case ZNormFunction.push:
                            fzFunc = "push ";
                            break;

                        case ZNormFunction.AltUaddr:
                            fzFunc = "AltUaddr ";
                            break;

                        case ZNormFunction.LRot0:
                            fzFunc = "LRot0 ";
                            break;

                        case ZNormFunction.LRot12:
                            fzFunc = "LRot12 ";
                            break;

                        case ZNormFunction.LRot8:
                            fzFunc = "LRot8 ";
                            break;

                        case ZNormFunction.LRot4:
                            fzFunc = "LRot4 ";
                            break;
                    }
                    break;
            }

            // SU reg write
            string suWriteStr = String.Empty;
            bool suWrite = enSU && Cin;
            bool suRead = enSU && !Cin;
            if (suWrite)
            {
                switch((int)fSfZ)
                {
                    case 0:
                    case 1:
                        suWriteStr = "STK<-";
                        break;

                    case 2:
                    case 3:
                        suWriteStr = String.Format("U{0:x2}<-", (rA << 4) | fZ);
                        break;
                }

                yBusIsSourceForDestination = true;
            }

            if(!yBusIsSourceForDestination && !xBusIsSourceForDestination)
            {
                suWriteStr = "Xbus<- ";

                // Y bus is implicitly used to provide an X bus value if nothing else is selected.
                yBusIsSourceForDestination = string.IsNullOrEmpty(xBusValue);
            }

            // MAR or MDR writes:
            string memWrite = String.Empty;

            if (mem)
            {
                if (cycle == 1)
                {
                    memWrite = "MAR<- ";
                }
                else if (cycle == 2)
                {
                    memWrite = "MDR<- ";
                }
                else if (cycle == -1)
                {
                    memWrite = "{MAR/MDR/MD} ";
                }

                yBusIsSourceForDestination = true;
            }

            //
            // The below is kind of messy because of conflation of the ALU with the Y-Bus way up above, etc.
            // Bear with me.
            //

            //
            // The Y Bus value is important and needs to be included in the disassembly if one or more of the
            // below are true:
            //  - A register assignment is taking place
            //  - The Y Bus is being used as a data source
            //  - A dispatch or branch involving the Y Bus or ALU is being invoked during this instruction.
            //            
            bool showyBusValue = (yBusBranch || yBusIsSourceForDestination || !aluNoWriteBack);

            //
            // The X Bus value is important and needs to be included in the disassembly if one or more of the
            // below are true:
            //  - The ALU isn't already using the X Bus as an input
            //  - The X Bus is being used as a data source
            //  - A dispatch or branch involving the X Bus is being invoked during this instruction.
            //
            bool showxBusValue = (xBusBranch || !AluNeedsXBus || xBusIsSourceForDestination);

            string disassembly = String.Format("{0}{1}{2}{3}{4}{5}{6} [{7:x3}]", 
                fxFunc, 
                fyFunc, 
                fzFunc,
                memWrite,
                suWriteStr,
                showxBusValue ? xBusValue : String.Empty, 
                showyBusValue ? yBusValue : String.Empty, 
                INIA);


            return disassembly;
        }        

        private string GetCarryMod()
        {
            string mod = String.Empty;
            bool add = (aF == AluFunction.RplusS);
            bool sub = (aF == AluFunction.RminusS || aF == AluFunction.SminusR);

            if (Cin & add)
            {
                mod = "+1";
            }
            else if (!Cin & sub)
            {
                mod = "-1";
            }

            return mod;
        }

        private string DisassembleXBusSource(int cycle)
        {
            string xBus = String.Empty;

            // Byte and/or Nibble.  In theory these are mutually exclusive,
            // but there's nothing that prevents them both from being coded at the same time.
            // If this happens, Byte takes precedence.
            if (fSfY == FunctionSelectFY.Byte)
            {
                xBus = String.Format("byte({0:x2})", ((fY << 4) | fZ));
            }

            if(fSfZ == FunctionSelectFZ.Nibble && fSfY != FunctionSelectFY.Byte)
            {
                xBus = String.Format("nibble({0:x1})", fZ);
            }
            else if (fSfZ == FunctionSelectFZ.IOXIn)
            {
                // IOXIn sources
                switch((ZIOXIn)fZ)
                {
                    case ZIOXIn.ReadEIdata:
                        xBus += "EIData";
                        break;

                    case ZIOXIn.ReadEStatus:
                        xBus += "EStatus";
                        break;

                    case ZIOXIn.ReadKIData:
                        xBus += "KIData";
                        break;

                    case ZIOXIn.ReadKStatus:
                        xBus += "KStatus";
                        break;

                    case ZIOXIn.ReadMStatus:
                        xBus += "MStatus";
                        break;

                    case ZIOXIn.ReadKTest:
                        xBus += "KTest";
                        break;

                    case ZIOXIn.ReadIOPIData:
                        xBus += "IOPIData";
                        break;

                    case ZIOXIn.ReadIOPStatus:
                        xBus += "IOPStatus";
                        break;

                    case ZIOXIn.ReadErrnIBnStkp:
                        xBus += "ErrnIBnStkP";
                        break;

                    case ZIOXIn.ReadRH:
                        xBus += String.Format("RH{0:x}", rB);
                        break;

                    case ZIOXIn.ReadibNA:
                        xBus += "ibNA";
                        break;

                    case ZIOXIn.ReadibLow:
                        xBus += "ibLow";
                        break;

                    case ZIOXIn.ReadibHigh:
                        xBus += "ibHigh";
                        break;

                    default:
                        xBus += ((ZIOXIn)fZ).ToString();
                        break;
                }
            }

            if (enSU && !Cin)   // Cin is 0 for reads
            {
                // SU read operations
                switch((int)fSfZ)
                {
                    case 0:
                    case 1:
                        xBus += "STK";
                        break;

                    case 2:
                    case 3:
                        xBus += String.Format("U{0:x2}", (rA << 4) | fZ);
                        break;
                }
            }

            if (mem && cycle == 3)
            {                
                xBus += "<-MD";
            }

            return xBus;
        }
    }
}
