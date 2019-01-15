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
using System.Runtime.InteropServices;

namespace D.IOP
{
    public enum InterruptType
    {
        RST7_5,
        RST6_5,
        RST5_5,
        TRAP,
        INTR,
    }

    /// <summary>
    /// Emulates the Intel 8085 processor.
    /// </summary>
    public class i8085
    {
        public i8085(I8085MemoryBus mem, I8085IOBus io)
        {
            _mem = mem;
            _io = io;

            InitializeInstructionTables();
            InitializeParityTable();

            Reset();
        }

        public void Reset()
        {
            _pc = 0;
            _sp = 0;
            _r.AF = 0;
            _r.BC = 0;
            _r.DE = 0;
            _r.HL = 0;

            _interruptMask = 0;

            _halted = false;
        }

        /// <summary>
        /// Raises the specified interrupt signal.
        /// NOTE: At this time this only handles interrupts used by the Star's IOP
        /// (RST7.5, 6.5, and 5.5).  TRAP and INTR are held low in the IOP and are never used.
        /// </summary>
        /// <param name="type"></param>
        public void RaiseExternalInterrupt(InterruptType type)
        {
            //
            // RST7.5 is edge-triggered, the others are level-triggered.
            // Set pending interrupt bits.
            //
            switch(type)
            {
                case InterruptType.RST7_5:
                    _interruptMask = (byte)(_interruptMask | P7_5);
                    break;

                case InterruptType.RST6_5:
                    _interruptMask = (byte)(_interruptMask | P6_5);
                    break;

                case InterruptType.RST5_5:
                    _interruptMask = (byte)(_interruptMask | P5_5);
                    break;

                default:
                    throw new NotImplementedException(String.Format("{0} interrupt not implemented.", type));
            }
        }

        public void ClearExternalInterrupt(InterruptType type)
        {
            //
            // RST7.5 is edge-triggered, the others are level-triggered.
            // Clear pending interrupt bits
            //
            switch (type)
            {
                case InterruptType.RST7_5:
                    throw new InvalidOperationException("Attempt to clear edge-triggered RST7.5 interrupt.");

                case InterruptType.RST6_5:
                    _interruptMask = (byte)(_interruptMask & ~P6_5);
                    break;

                case InterruptType.RST5_5:
                    _interruptMask = (byte)(_interruptMask & ~P5_5);
                    break;

                default:
                    throw new NotImplementedException(String.Format("{0} interrupt not implemented.", type));
            }
        }

        public bool Halted
        {
            get { return _halted; }
        }

        public byte A
        {
            get { return _r.A; }
        }

        public byte F
        {
            get { return _r.F; }
        }

        public byte B
        {
            get { return _r.B; }
        }

        public byte C
        {
            get { return _r.C; }
        }

        public byte D
        {
            get { return _r.D; }
        }

        public byte E
        {
            get { return _r.E; }
        }

        public byte H
        {
            get { return _r.H; }
        }

        public byte L
        {
            get { return _r.L; }
        }

        public ushort PC
        {
            get { return _pc; }
        }

        public ushort SP
        {
            get { return _sp; }
        }

        public ushort AF
        {
            get { return _r.AF; }
        }

        public ushort BC
        {
            get { return _r.BC; }
        }

        public ushort DE
        {
            get { return _r.DE; }
        }

        public ushort HL
        {
            get { return _r.HL; }
        }

        public string Disassemble(ushort address)
        {
            InstructionData i = _instructionData[_mem.ReadByte(address++)];

            if (i.Size == 1)
            {
                return i.Mnemonic;
            }
            else if (i.Size == 2)
            {
                return String.Format(i.Mnemonic, _mem.ReadByte(address));
            }
            else
            {
                return String.Format(i.Mnemonic, _mem.ReadWord(address));
            }
        }

        /// <summary>
        /// Executes a single 8085 instruction at the current PC.
        /// Returns the number of clock cycles consumed by the operation.
        /// </summary>
        /// <returns></returns>
        public int Execute()
        {
            //
            // Handle any pending interrupts if enabled and not masked.
            //
            if ((_interruptMask & IE) != 0)
            {
                //
                // Interrupt priority is (from highest to lowest) TRAP,7.5,6.5,5.5,INTR
                // (TRAP and INTR are not implemented.)
                // Choose the highest one and vector to the right place.
                //
                if ((_interruptMask & P7_5) != 0 && (_interruptMask & I7_5) == 0)
                {
                    Restore(0x3c);

                    // Clear the IE mask
                    _interruptMask = (byte)(_interruptMask & ~IE);

                    // The RST7.5 flip flop is reset when the interrupt is recognized.
                    _interruptMask = (byte)(_interruptMask & ~P7_5);
                }
                else if ((_interruptMask & P6_5) != 0 && (_interruptMask & I6_5) == 0)
                {
                    Restore(0x34);

                    // Clear the IE mask
                    _interruptMask = (byte)(_interruptMask & ~IE);
                }
                else if ((_interruptMask & P5_5) != 0 && (_interruptMask & I5_5) == 0)
                {
                    Restore(0x2c);

                    // Clear the IE mask
                    _interruptMask = (byte)(_interruptMask & ~IE);
                }
            }


            InstructionData i = _instructionData[_mem.ReadByte(_pc++)];

            ushort arg = 0;
            if (i.Size == 2)
            {
                arg = _mem.ReadByte(_pc);
                _pc++;
            }
            else if (i.Size == 3)
            {
                arg = _mem.ReadWord(_pc);
                _pc += 2;
            }

            return i.Executor(i.Opcode, arg) ? i.Cycles2 : i.Cycles1;
        }

        //
        // Instruction implementation
        //
        private bool ADD(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = arg;
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            int temp = _r.A + src;
            // test for full-borrow
            _r.F_CY = (temp & 0x100) != 0;

            // test for half-borrow
            _r.F_AC = (((_r.A & 0x0f) + (src & 0x0f)) & 0x10) != 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.A = (byte)temp;

            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ADC(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = arg;
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            int temp = _r.A + src + (_r.F_CY ? 1 : 0);

            // test for half-carry
            _r.F_AC = (((_r.A & 0x0f) + (src & 0x0f) + (_r.F_CY ? 1 : 0)) & 0x10) != 0;

            // test for carry
            _r.F_CY = (temp & 0x100) != 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;
            _r.A = (byte)temp;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ACI(byte op, ushort arg)
        {
            int temp = _r.A + arg + (_r.F_CY ? 1 : 0); ;

            // test for half-carry
            _r.F_AC = (((_r.A & 0x0f) + (arg & 0x0f) + (_r.F_CY ? 1 : 0)) & 0x10) != 0;

            // test for carry
            _r.F_CY = (temp & 0x100) != 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;
            _r.A = (byte)temp;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ADI(byte op, ushort arg)
        {
            int temp = _r.A + arg;

            // test for half-carry
            _r.F_AC = (((_r.A & 0x0f) + (arg & 0x0f)) & 0x10) != 0;

            // test for carry
            _r.F_CY = (temp & 0x100) != 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;        
            _r.A = (byte)temp;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ANA(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            _r.A &= (byte)src;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ANI(byte op, ushort arg)
        {
            _r.A &= (byte)arg;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool CALL(byte op, ushort arg)
        {
            Push(_pc);
            _pc = arg;

            return false;
        }

        /// <summary>
        /// Conditional RET
        /// </summary>
        /// <param name="op"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private bool CALLC(byte op, ushort arg)
        {
            bool call = false;

            switch ((op & 0x38) >> 3)
            {
                case 0:     // CNZ
                    call = !_r.F_Z;
                    break;

                case 1:     // CZ
                    call = _r.F_Z;
                    break;

                case 2:     // CNC
                    call = !_r.F_CY;
                    break;

                case 3:     // CC
                    call = _r.F_CY;
                    break;

                case 4:     // CPO:
                    call = !_r.F_P;
                    break;

                case 5:     // CPE:
                    call = _r.F_P;
                    break;

                case 6:     // CP:
                    call = !_r.F_S;
                    break;

                case 7:     // CM
                    call = _r.F_S;
                    break;
            }

            if (call)
            {
                Push(_pc);
                _pc = arg;
            }

            // no flags affected

            return false;
        }

        private bool CMA(byte op, ushort arg)
        {
            _r.A = (byte)(~_r.A);

            // no flags affected
            return false;
        }

        private bool CMC(byte op, ushort arg)
        {
            _r.F_CY = !_r.F_CY;

            return false;
        }

        private bool CMP(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            int temp = _r.A - src;
            // test for full-borrow
            _r.F_CY = (temp < 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (src & 0x0f)) < 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.F_P = _parityTable[(byte)temp];

            return false;
        }

        private bool CPI(byte op, ushort arg)
        {            
            int temp = _r.A - (byte)arg;
            // test for full-borrow
            _r.F_CY = (temp < 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (arg & 0x0f)) < 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.F_P = _parityTable[(byte)temp];

            return false;
        }

        private bool DAA(byte op, ushort arg)
        {
            if ((_r.A & 0xf) > 9 ||
                _r.F_AC)
            {
                _r.A += 6;
            }

            if (((_r.A & 0xf0) >> 4) > 9 ||
                _r.F_CY)
            {
                _r.A += (6 << 4);
            }

            throw new NotImplementedException("DAA is not implemented yet.");

            return false;
        }

        private bool DAD(byte op, ushort arg)
        {
            int addend = 0;
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    addend = _r.BC;
                    break;

                case 1:
                    addend = _r.DE;
                    break;

                case 2:
                    addend = _r.HL;
                    break;

                case 3:
                    addend = _sp;
                    break;
            }

            _r.F_CY = (_r.HL + addend > 0xffff);

            _r.HL += (ushort)addend;

            return false;
        }

        private bool DCR(byte op, ushort arg)
        {
            byte res = 0;
            switch ((op & 0x38) >> 3)
            {
                case 0:
                    res = --_r.B;
                    break;
                case 1:
                    res = --_r.C;
                    break;
                case 2:
                    res = --_r.D;
                    break;
                case 3:
                    res = --_r.E;
                    break;
                case 4:
                    res = --_r.H;
                    break;
                case 5:
                    res = --_r.L;
                    break;
                case 6:
                    res = (byte)(_mem.ReadByte(_r.HL) - 1);
                    _mem.WriteByte(_r.HL, (byte)res);
                    break;
                case 7:
                    res = --_r.A;
                    break;
            }

            // carry not affected
            _r.F_Z = (res == 0);
            _r.F_S = ((res & 0x80) != 0);
            _r.F_P = _parityTable[res];
            _r.F_AC = ((res & 0xf) == 0xf);       // just subtracted 1, if low nybble is 0xf, there was a carry out.

            return false;
        }

        private bool DCX(byte op, ushort arg)
        {
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    _r.BC--;
                    break;

                case 1:
                    _r.DE--;
                    break;

                case 2:
                    _r.HL--;
                    break;

                case 3:
                    _sp--;
                    break;
            }

            // no flags affected.

            return false;
        }

        private bool DI(byte op, ushort arg)
        {
            // Clear the Interrupt Enable flag
            _interruptMask = (byte)(_interruptMask & ~(IE));
            return false;
        }

        private bool EI(byte op, ushort arg)
        {
            // Set the Interrupt Enable flag
            _interruptMask |= (byte)IE;
            return false;
        }

        private bool HLT(byte op, ushort arg)
        {
            _halted = true;
            return false;
        }

        private bool IN(byte op, ushort arg)
        {
            _r.A = _io.In((byte)arg);

            return false;
        }

        private bool INR(byte op, ushort arg)
        {
            byte res = 0;
            switch ((op & 0x38) >> 3)
            {
                case 0:
                    res = ++_r.B;
                    break;
                case 1:
                    res = ++_r.C;
                    break;
                case 2:
                    res = ++_r.D;
                    break;
                case 3:
                    res = ++_r.E;
                    break;
                case 4:
                    res = ++_r.H;
                    break;
                case 5:
                    res = ++_r.L;
                    break;
                case 6:
                    res = (byte)(_mem.ReadByte(_r.HL) + 1);
                    _mem.WriteByte(_r.HL, (byte)res);
                    break;
                case 7:
                    res = ++_r.A;
                    break;
            }

            // carry not affected
            _r.F_Z = (res == 0);
            _r.F_S = ((res & 0x80) != 0);
            _r.F_P = _parityTable[res];
            _r.F_AC = ((res & 0xf) == 0);       // just added 1, if low nybble is zero, there was a carry out.

            return false;
        }

        private bool Invalid(byte op, ushort arg)
        {
            // For now we'll throw
            throw new InvalidOperationException(
                String.Format("Invalid 8085 instruction {0:x2}", op));
        }

        private bool INX(byte op, ushort arg)
        {
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    _r.BC++;
                    break;

                case 1:
                    _r.DE++;
                    break;

                case 2:
                    _r.HL++;
                    break;

                case 3:
                    _sp++;
                    break;
            }

            // no flags affected.

            return false;
        }

        private bool JMP(byte op, ushort arg)
        {
            bool test = false;
            switch(op & 0x3f)
            {
                case 0x02: // jnz
                    test = !_r.F_Z;
                    break;

                case 0x03: // jmp
                    test = true;
                    break;

                case 0x0a: // jz
                    test = _r.F_Z;
                    break;

                case 0x12: // jnc
                    test = !_r.F_CY;
                    break;

                case 0x1a: // jc
                    test = _r.F_CY;
                    break;

                case 0x22: // jpo
                    test = !_r.F_P;
                    break;

                case 0x2a: // jpe
                    test = _r.F_P;
                    break;

                case 0x32: // jp
                    test = !_r.F_S;
                    break;

                case 0x3a: // jm
                    test = _r.F_S;
                    break;

                default:
                    throw new InvalidOperationException("Unhandled JMP instruction.");
            }

            if (test)
            {
                _pc = arg;
            }

            return test;
        }

        private bool LDA(byte op, ushort arg)
        {
            _r.A = _mem.ReadByte(arg);

            return false;
        }

        private bool LDAX(byte op, ushort arg)
        {
            switch ((op & 0x10) >> 4)
            {
                case 0:
                    _r.A = _mem.ReadByte(_r.BC);
                    break;

                case 1:
                    _r.A = _mem.ReadByte(_r.DE);
                    break;
            }

            // no flags affected.

            return false;
        }
        
        private bool LHLD(byte op, ushort arg)
        {
            _r.HL = _mem.ReadWord(arg);

            return false;
        }

        private bool LXI(byte op, ushort arg)
        {
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    _r.BC = arg;
                    break;

                case 1:
                    _r.DE = arg;
                    break;

                case 2:
                    _r.HL = arg;
                    break;

                case 3:
                    _sp = arg;
                    break;
            }

            // no flags affected.

            return false;
        }

        private bool MOV(byte op, ushort arg)
        {
            int src = op & 0x7;
            int dst = (op >> 3) & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            switch (dst)
            {
                case 0:
                    _r.B = (byte)src;
                    break;
                case 1:
                    _r.C = (byte)src;
                    break;
                case 2:
                    _r.D = (byte)src;
                    break;
                case 3:
                    _r.E = (byte)src;
                    break;
                case 4:
                    _r.H = (byte)src;
                    break;
                case 5:
                    _r.L = (byte)src;
                    break;
                case 6:
                    _mem.WriteByte(_r.HL, (byte)src);
                    break;
                case 7:
                    _r.A = (byte)src;
                    break;
            }

            // No flags affected.

            return false;
        }

        private bool MVI(byte op, ushort arg)
        {
            switch ((op & 0x38) >> 3)
            {
                case 0:
                    _r.B = (byte)arg;
                    break;
                case 1:
                    _r.C = (byte)arg;
                    break;
                case 2:
                    _r.D = (byte)arg;
                    break;
                case 3:
                    _r.E = (byte)arg;
                    break;
                case 4:
                    _r.H = (byte)arg;
                    break;
                case 5:
                    _r.L = (byte)arg;
                    break;
                case 6:
                    _mem.WriteByte(_r.HL, (byte)arg);
                    break;
                case 7:
                    _r.A = (byte)arg;
                    break;
            }

            // no flags affected.
            return false;
        }

        private bool NOP(byte op, ushort arg)
        {
            // Do nothing at all.
            return false;
        }

        private bool OUT(byte op, ushort arg)
        {
            _io.Out((byte)arg, _r.A);
            return false;
        }

        private bool ORA(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            _r.A |= (byte)src;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool ORI(byte op, ushort arg)
        {
            _r.A |= (byte)arg;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool PCHL(byte op, ushort arg)
        {
            _pc = _r.HL;
            return false;
        }

        private bool POP(byte op, ushort arg)
        {
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    _r.BC = Pop();
                    break;

                case 1:
                    _r.DE = Pop();
                    break;

                case 2:
                    _r.HL = Pop();
                    break;

                case 3:
                    _r.AF = Pop();
                    break;
            }

            // no flags affected.
            return false;
        }

        private bool PUSH(byte op, ushort arg)
        {
            switch ((op & 0x30) >> 4)
            {
                case 0:
                    Push(_r.BC);
                    break;

                case 1:
                    Push(_r.DE);
                    break;

                case 2:
                    Push(_r.HL);
                    break;

                case 3:
                    Push(_r.AF);
                    break;
            }

            // no flags affected.
            return false;
        }

        private bool RAL(byte op, ushort arg)
        {
            bool newCarry = (_r.A & 0x80) != 0;

            _r.A = (byte)((_r.A << 1) | (_r.F_CY ? 1 : 0));
            _r.F_CY = newCarry;

            return false;
        }

        private bool RAR(byte op, ushort arg)
        {
            bool newCarry = (_r.A & 0x01) != 0;

            _r.A = (byte)((_r.A >> 1) | (_r.F_CY ? 0x80 : 0));
            _r.F_CY = newCarry;

            return false;
        }

        /// <summary>
        /// Unconditional RET
        /// </summary>
        /// <param name="op"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private bool RET(byte op, ushort arg)
        {            
            _pc = Pop();            

            // no flags affected

            return false;
        }

        /// <summary>
        /// Conditional RET
        /// </summary>
        /// <param name="op"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private bool RETC(byte op, ushort arg)
        {
            bool ret = false;

            switch ((op & 0x38) >> 3)
            {
                case 0:     // RNZ
                    ret = !_r.F_Z;
                    break;

                case 1:     // RZ
                    ret = _r.F_Z;
                    break;

                case 2:     // RNC
                    ret = !_r.F_CY;
                    break;

                case 3:     // RC
                    ret = _r.F_CY;
                    break;

                case 4:     // RPO:
                    ret = !_r.F_P;
                    break;

                case 5:     // RPE:
                    ret = _r.F_P;
                    break;

                case 6:     // RP:
                    ret = !_r.F_S;
                    break;

                case 7:     // RM
                    ret = _r.F_S;
                    break;
            }

            if (ret)
            {
                _pc = Pop();
            }

            // no flags affected

            return false;
        }

        private bool RIM(byte op, ushort arg)
        {
            _r.A = _interruptMask;

            return false;
        }

        private bool RLC(byte op, ushort arg)
        {
            _r.F_CY = (_r.A & 0x80) != 0;

            _r.A = (byte)((_r.A << 1) | (_r.F_CY ? 1 : 0));

            return false;
        }

        private bool RRC(byte op, ushort arg)
        {
            _r.F_CY = (_r.A & 0x01) != 0;

            _r.A = (byte)((_r.A >> 1) | (_r.F_CY ? 0x80 : 0));

            return false;
        }

        private bool RST(byte op, ushort arg)
        {
            Restore((ushort)(op & 0x38));
            return false;
        }

        private bool SBB(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            int temp = _r.A - src - (_r.F_CY ? 1 : 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (src & 0x0f) - (_r.F_CY ? 1 : 0)) < 0;

            // test for full-borrow
            _r.F_CY = (temp < 0);

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.A = (byte)temp;

            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool SBI(byte op, ushort arg)
        {
            int temp = _r.A - (byte)arg - (_r.F_CY ? 1 : 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (arg & 0x0f) - (_r.F_CY ? 1 : 0)) < 0;

            // test for full-borrow
            _r.F_CY = (temp < 0);

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.A = (byte)temp;

            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool SHLD(byte op, ushort arg)
        {
            _mem.WriteWord(arg, _r.HL);

            // no flags affected
            return false;
        }

        private bool SIM(byte op, ushort arg)
        {
            if ((_r.A & 0x8) != 0)
            {
                // Set new interrupt mask
                _interruptMask = (byte)((_interruptMask & 0xf8) | (_r.A & 0x7));
            }

            if ((_r.A & 0x10) != 0)
            {
                //
                // Clear pending interrupt for RST7.5
                //
                _interruptMask = (byte)(_interruptMask & ~P7_5);
            }
            
            // TODO: serial output (not used by IOP)
            
            return false;
        }

        private bool SPHL(byte op, ushort arg)
        {
            _sp = _r.HL;
            return false;
        }

        private bool STA(byte op, ushort arg)
        {
            _mem.WriteByte(arg, _r.A);

            return false;
        }

        private bool STAX(byte op, ushort arg)
        {
            switch ((op & 0x10) >> 4)
            {
                case 0:
                    _mem.WriteByte(_r.BC, _r.A);
                    break;

                case 1:
                    _mem.WriteByte(_r.DE, _r.A);
                    break;
            }

            // no flags affected.

            return false;
        }

        private bool STC(byte op, ushort arg)
        {
            _r.F_CY = true;

            return false;
        }

        private bool SUB(byte op, ushort arg)
        {
            int src = op & 0x7;
            
            switch(src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            int temp = _r.A - src;
            // test for full-borrow
            _r.F_CY = (temp < 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (src & 0x0f)) < 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.A = (byte)temp;

            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool SUI(byte op, ushort arg)
        {            
            int temp = _r.A - (byte)arg;
            // test for full-borrow
            _r.F_CY = (temp < 0);

            // test for half-borrow
            _r.F_AC = (sbyte)((_r.A & 0x0f) - (arg & 0x0f)) < 0;

            // zero?
            _r.F_Z = (byte)temp == 0;

            // negative?
            _r.F_S = (temp & 0x80) != 0;

            _r.A = (byte)temp;

            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool XCHG(byte op, ushort arg)
        {
            ushort temp = _r.HL;
            _r.HL = _r.DE;
            _r.DE = temp;

            return false;
        }        

        private bool XRA(byte op, ushort arg)
        {
            int src = op & 0x7;

            switch (src)
            {
                case 0:
                    src = _r.B;
                    break;
                case 1:
                    src = _r.C;
                    break;
                case 2:
                    src = _r.D;
                    break;
                case 3:
                    src = _r.E;
                    break;
                case 4:
                    src = _r.H;
                    break;
                case 5:
                    src = _r.L;
                    break;
                case 6:
                    src = _mem.ReadByte(_r.HL);
                    break;
                case 7:
                    src = _r.A;
                    break;
            }

            _r.A ^= (byte)src;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool XRI(byte op, ushort arg)
        {
            _r.A ^= (byte)arg;

            _r.F_CY = false;
            _r.F_AC = false;
            _r.F_Z = _r.A == 0;
            _r.F_S = (_r.A & 0x80) != 0;
            _r.F_P = _parityTable[_r.A];

            return false;
        }

        private bool XTHL(byte op, ushort arg)
        {
            ushort temp = Pop();
            Push(_r.HL);
            _r.HL = temp;

            return false;
        }

        //
        // Helper routines
        //
        private void Push(ushort v)
        {
            _sp -= 2;
            _mem.WriteWord(_sp, v);
        }

        private ushort Pop()
        {
            ushort v = _mem.ReadWord(_sp);
            _sp += 2;

            return v;
        }

        private void Restore(ushort addr)
        {
            Push(_pc);
            _pc = addr;
        }

        // Processor registers
        private ushort _pc;
        private ushort _sp;
        private RegisterFile _r;

        /// <summary>
        /// RegisterFile has an explict layout to simplify logic around
        /// the 8-bit half-registers, etc.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct RegisterFile
        {
            //
            // AF register pair
            //
            [FieldOffset(1)]
            public byte A;

            [FieldOffset(0)]
            public byte F;

            [FieldOffset(0)]
            public ushort AF;

            //
            // F flag bits
            // TODO: would be faster to simply save boolean values here
            //       and only modify F when F is used.
            //
            public bool F_CY
            {
                get { return (F & 0x1) != 0; }
                set { F = (byte)(value ? (F | 0x1) : (F & 0xfe)); }
            }

            public bool F_P
            {
                get { return (F & 0x4) != 0; }
                set { F = (byte)(value ? (F | 0x4) : (F & 0xfb)); }
            }

            public bool F_AC
            {
                get { return (F & 0x10) != 0; }
                set { F = (byte)(value ? (F | 0x10) : (F & 0xef)); }
            }

            public bool F_Z
            {
                get { return (F & 0x40) != 0; }
                set { F = (byte)(value ? (F | 0x40) : (F & 0xbf)); }
            }

            public bool F_S
            {
                get { return (F & 0x80) != 0; }
                set { F = (byte)(value ? (F | 0x80) : (F & 0x7f)); }
            }

            //
            // BC register pair
            //
            [FieldOffset(3)]
            public byte B;

            [FieldOffset(2)]
            public byte C;

            [FieldOffset(2)]
            public ushort BC;

            //
            // DE register pair
            //
            [FieldOffset(5)]
            public byte D;

            [FieldOffset(4)]
            public byte E;

            [FieldOffset(4)]
            public ushort DE;

            //
            // HL register pair
            //
            [FieldOffset(7)]
            public byte H;

            [FieldOffset(6)]
            public byte L;

            [FieldOffset(6)]
            public ushort HL;
        }

        // Interface to memory
        private I8085MemoryBus _mem;

        // Interface to IO
        private I8085IOBus _io;

        // Interrupt mask and control bits
        private byte _interruptMask;

        // Interrupt mask bits
        private const int I5_5 = 0x01;
        private const int I6_5 = 0x02;
        private const int I7_5 = 0x04;
        private const int IE =   0x08;
        private const int P5_5 = 0x10;
        private const int P6_5 = 0x20;
        private const int P7_5 = 0x40;
        private const int SID =  0x80;

        // Whether the CPU has been halted vi HLT
        private bool _halted;

        /// <summary>
        /// Delegate for an instruction execution
        /// </summary>
        /// <returns></returns>
        private delegate bool Executor(byte op, ushort arg);

        private bool[] _parityTable;

        private void InitializeParityTable()
        {
            _parityTable = new bool[256];

            for (int i = 0; i < 256; i++)
            {
                int oneBits = 0;

                for (int j = 1; j < 0x100; j = j << 1)
                {
                    if ((i & j) != 0)
                    {
                        oneBits++;
                    }
                }

                _parityTable[i] = ((oneBits % 2) == 0);
            }
        }

        /// <summary>
        /// Represents data for a given 8085 opcode
        /// </summary>
        private sealed class InstructionData
        {
            public InstructionData(byte opcode, string mnemonic, ushort size, int cycles1, int cycles2, Executor executor)
            {
                Opcode = opcode;
                Mnemonic = mnemonic;
                Size = size;
                Cycles1 = cycles1;
                Cycles2 = cycles2;
                Executor = executor;
            }

            public InstructionData(byte opcode, string mnemonic, ushort size, int cycles, Executor executor)
            {
                Opcode = opcode;
                Mnemonic = mnemonic;
                Size = size;
                Cycles1 = cycles;
                Cycles2 = cycles;
                Executor = executor;
            }

            public readonly byte Opcode;
            public readonly string Mnemonic;
            public readonly ushort Size;
            public readonly int Cycles1;
            public readonly int Cycles2;
            public readonly Executor Executor;
        }

        private void InitializeInstructionTables()
        {
            _instructionData = new InstructionData[] {
                new InstructionData(0x00, "NOP", 1, 4, new Executor(NOP)),
                new InstructionData(0x01, "LXI B,${0:x4}", 3, 10, new Executor(LXI)),
                new InstructionData(0x02, "STAX B", 1, 7, new Executor(STAX)),
                new InstructionData(0x03, "INX B", 1, 6, new Executor(INX)),
                new InstructionData(0x04, "INR B", 1, 4, new Executor(INR)),
                new InstructionData(0x05, "DCR B", 1, 4, new Executor(DCR)),
                new InstructionData(0x06, "MVI B,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x07, "RLC", 1, 4, new Executor(RLC)),
                new InstructionData(0x08, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0x09, "DAD B", 1, 10, new Executor(DAD)),
                new InstructionData(0x0a, "LDAX B", 1, 7, new Executor(LDAX)),
                new InstructionData(0x0b, "DCX B", 1, 6, new Executor(DCX)),
                new InstructionData(0x0c, "INR C", 1, 4, new Executor(INR)),
                new InstructionData(0x0d, "DCR C", 1, 4, new Executor(DCR)),
                new InstructionData(0x0e, "MVI C,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x0f, "RRC", 1, 4, new Executor(RRC)),

                new InstructionData(0x10, "Invalid", 1, 4, new Executor(Invalid)),
                new InstructionData(0x11, "LXI D,${0:x4}", 3, 10, new Executor(LXI)),
                new InstructionData(0x12, "STAX D", 1, 7, new Executor(STAX)),
                new InstructionData(0x13, "INX D", 1, 6, new Executor(INX)),
                new InstructionData(0x14, "INR D", 1, 4, new Executor(INR)),
                new InstructionData(0x15, "DCR D", 1, 4, new Executor(DCR)),
                new InstructionData(0x16, "MVI D,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x17, "RAL", 1, 4, new Executor(RAL)),
                new InstructionData(0x18, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0x19, "DAD D", 1, 10, new Executor(DAD)),
                new InstructionData(0x1a, "LDAX D", 1, 7, new Executor(LDAX)),
                new InstructionData(0x1b, "DCX D", 1, 6, new Executor(DCX)),
                new InstructionData(0x1c, "INR E", 1, 4, new Executor(INR)),
                new InstructionData(0x1d, "DCR E", 1, 4, new Executor(DCR)),
                new InstructionData(0x1e, "MVI E,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x1f, "RAR", 1, 4, new Executor(RAR)),

                new InstructionData(0x20, "RIM", 1, 4, new Executor(RIM)),
                new InstructionData(0x21, "LXI H,${0:x4}", 3, 10, new Executor(LXI)),
                new InstructionData(0x22, "SHLD $({0:x4})", 3, 16, new Executor(SHLD)),
                new InstructionData(0x23, "INX H", 1, 6, new Executor(INX)),
                new InstructionData(0x24, "INR H", 1, 4, new Executor(INR)),
                new InstructionData(0x25, "DCR H", 1, 4, new Executor(DCR)),
                new InstructionData(0x26, "MVI H,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x27, "DAA", 1, 4, new Executor(DAA)),
                new InstructionData(0x28, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0x29, "DAD H", 1, 10, new Executor(DAD)),
                new InstructionData(0x2a, "LHLD (${0:x4})", 3, 16, new Executor(LHLD)),
                new InstructionData(0x2b, "DCX H", 1, 6, new Executor(DCX)),
                new InstructionData(0x2c, "INR L", 1, 4, new Executor(INR)),
                new InstructionData(0x2d, "DCR L", 1, 4, new Executor(DCR)),
                new InstructionData(0x2e, "MVI L,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x2f, "CMA", 1, 4, new Executor(CMA)),

                new InstructionData(0x30, "SIM", 1, 4, new Executor(SIM)),
                new InstructionData(0x31, "LXI SP,${0:x4}", 3, 10, new Executor(LXI)),
                new InstructionData(0x32, "STA (${0:x4})", 3, 13, new Executor(STA)),
                new InstructionData(0x33, "INX SP", 1, 6, new Executor(INX)),
                new InstructionData(0x34, "INR M", 1, 10, new Executor(INR)),
                new InstructionData(0x35, "DCR M", 1, 10, new Executor(DCR)),
                new InstructionData(0x36, "MVI M,${0:x2}", 2, 10, new Executor(MVI)),
                new InstructionData(0x37, "STC", 1, 4, new Executor(STC)),
                new InstructionData(0x38, "Invalid", 2, 0, new Executor(Invalid)),
                new InstructionData(0x39, "DAD SP", 1, 10, new Executor(DAD)),
                new InstructionData(0x3a, "LDA (${0:x4})", 3, 13, new Executor(LDA)),
                new InstructionData(0x3b, "DCX SP", 1, 6, new Executor(DCX)),
                new InstructionData(0x3c, "INR A", 1, 4, new Executor(INR)),
                new InstructionData(0x3d, "DCR A", 1, 4, new Executor(DCR)),
                new InstructionData(0x3e, "MVI A,${0:x2}", 2, 7, new Executor(MVI)),
                new InstructionData(0x3f, "CMC", 1, 4, new Executor(CMC)),

                new InstructionData(0x40, "MOV B,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x41, "MOV B,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x42, "MOV B,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x43, "MOV B,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x44, "MOV B,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x45, "MOV B,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x46, "MOV B,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x47, "MOV B,A", 1, 4, new Executor(MOV)),
                new InstructionData(0x48, "MOV C,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x49, "MOV C,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x4a, "MOV C,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x4b, "MOV C,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x4c, "MOV C,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x4d, "MOV C,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x4e, "MOV C,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x4f, "MOV C,A", 1, 4, new Executor(MOV)),

                new InstructionData(0x50, "MOV D,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x51, "MOV D,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x52, "MOV D,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x53, "MOV D,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x54, "MOV D,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x55, "MOV D,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x56, "MOV D,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x57, "MOV D,A", 1, 4, new Executor(MOV)),
                new InstructionData(0x58, "MOV E,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x59, "MOV E,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x5a, "MOV E,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x5b, "MOV E,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x5c, "MOV E,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x5d, "MOV E,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x5e, "MOV E,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x5f, "MOV E,A", 1, 4, new Executor(MOV)),

                new InstructionData(0x60, "MOV H,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x61, "MOV H,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x62, "MOV H,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x63, "MOV H,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x64, "MOV H,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x65, "MOV H,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x66, "MOV H,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x67, "MOV H,A", 1, 4, new Executor(MOV)),
                new InstructionData(0x68, "MOV L,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x69, "MOV L,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x6a, "MOV L,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x6b, "MOV L,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x6c, "MOV L,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x6d, "MOV L,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x6e, "MOV L,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x6f, "MOV L,A", 1, 4, new Executor(MOV)),

                new InstructionData(0x70, "MOV M,B", 1, 7, new Executor(MOV)),
                new InstructionData(0x71, "MOV M,C", 1, 7, new Executor(MOV)),
                new InstructionData(0x72, "MOV M,D", 1, 7, new Executor(MOV)),
                new InstructionData(0x73, "MOV M,E", 1, 7, new Executor(MOV)),
                new InstructionData(0x74, "MOV M,H", 1, 7, new Executor(MOV)),
                new InstructionData(0x75, "MOV M,L", 1, 7, new Executor(MOV)),
                new InstructionData(0x76, "HLT", 1, 5, new Executor(HLT)),
                new InstructionData(0x77, "MOV M,A", 1, 7, new Executor(MOV)),
                new InstructionData(0x78, "MOV A,B", 1, 4, new Executor(MOV)),
                new InstructionData(0x79, "MOV A,C", 1, 4, new Executor(MOV)),
                new InstructionData(0x7a, "MOV A,D", 1, 4, new Executor(MOV)),
                new InstructionData(0x7b, "MOV A,E", 1, 4, new Executor(MOV)),
                new InstructionData(0x7c, "MOV A,H", 1, 4, new Executor(MOV)),
                new InstructionData(0x7d, "MOV A,L", 1, 4, new Executor(MOV)),
                new InstructionData(0x7e, "MOV A,M", 1, 7, new Executor(MOV)),
                new InstructionData(0x7f, "MOV A,A", 1, 4, new Executor(MOV)),

                new InstructionData(0x80, "ADD B", 1, 4, new Executor(ADD)),
                new InstructionData(0x81, "ADD C", 1, 4, new Executor(ADD)),
                new InstructionData(0x82, "ADD D", 1, 4, new Executor(ADD)),
                new InstructionData(0x83, "ADD E", 1, 4, new Executor(ADD)),
                new InstructionData(0x84, "ADD H", 1, 4, new Executor(ADD)),
                new InstructionData(0x85, "ADD L", 1, 4, new Executor(ADD)),
                new InstructionData(0x86, "ADD M", 1, 7, new Executor(ADD)),
                new InstructionData(0x87, "ADD A", 1, 4, new Executor(ADD)),
                new InstructionData(0x88, "ADC B", 1, 4, new Executor(ADC)),
                new InstructionData(0x89, "ADC C", 1, 4, new Executor(ADC)),
                new InstructionData(0x8a, "ADC D", 1, 4, new Executor(ADC)),
                new InstructionData(0x8b, "ADC E", 1, 4, new Executor(ADC)),
                new InstructionData(0x8c, "ADC H", 1, 4, new Executor(ADC)),
                new InstructionData(0x8d, "ADC L", 1, 4, new Executor(ADC)),
                new InstructionData(0x8e, "ADC M", 1, 7, new Executor(ADC)),
                new InstructionData(0x8f, "ADC A", 1, 4, new Executor(ADC)),

                new InstructionData(0x90, "SUB B", 1, 4, new Executor(SUB)),
                new InstructionData(0x91, "SUB C", 1, 4, new Executor(SUB)),
                new InstructionData(0x92, "SUB D", 1, 4, new Executor(SUB)),
                new InstructionData(0x93, "SUB E", 1, 4, new Executor(SUB)),
                new InstructionData(0x94, "SUB H", 1, 4, new Executor(SUB)),
                new InstructionData(0x95, "SUB L", 1, 4, new Executor(SUB)),
                new InstructionData(0x96, "SUB M", 1, 7, new Executor(SUB)),
                new InstructionData(0x97, "SUB A", 1, 4, new Executor(SUB)),
                new InstructionData(0x98, "SBB B", 1, 4, new Executor(SBB)),
                new InstructionData(0x99, "SBB C", 1, 4, new Executor(SBB)),
                new InstructionData(0x9a, "SBB D", 1, 4, new Executor(SBB)),
                new InstructionData(0x9b, "SBB E", 1, 4, new Executor(SBB)),
                new InstructionData(0x9c, "SBB H", 1, 4, new Executor(SBB)),
                new InstructionData(0x9d, "SBB L", 1, 4, new Executor(SBB)),
                new InstructionData(0x9e, "SBB M", 1, 7, new Executor(SBB)),
                new InstructionData(0x9f, "SBB A", 1, 4, new Executor(SBB)),

                new InstructionData(0xa0, "ANA B", 1, 4, new Executor(ANA)),
                new InstructionData(0xa1, "ANA C", 1, 4, new Executor(ANA)),
                new InstructionData(0xa2, "ANA D", 1, 4, new Executor(ANA)),
                new InstructionData(0xa3, "ANA E", 1, 4, new Executor(ANA)),
                new InstructionData(0xa4, "ANA H", 1, 4, new Executor(ANA)),
                new InstructionData(0xa5, "ANA L", 1, 4, new Executor(ANA)),
                new InstructionData(0xa6, "ANA M", 1, 7, new Executor(ANA)),
                new InstructionData(0xa7, "ANA A", 1, 4, new Executor(ANA)),
                new InstructionData(0xa8, "XRA B", 1, 4, new Executor(XRA)),
                new InstructionData(0xa9, "XRA C", 1, 4, new Executor(XRA)),
                new InstructionData(0xaa, "XRA D", 1, 4, new Executor(XRA)),
                new InstructionData(0xab, "XRA E", 1, 4, new Executor(XRA)),
                new InstructionData(0xac, "XRA H", 1, 4, new Executor(XRA)),
                new InstructionData(0xad, "XRA L", 1, 4, new Executor(XRA)),
                new InstructionData(0xae, "XRA M", 1, 7, new Executor(XRA)),
                new InstructionData(0xaf, "XRA A", 1, 4, new Executor(XRA)),

                new InstructionData(0xb0, "ORA B", 1, 4, new Executor(ORA)),
                new InstructionData(0xb1, "ORA C", 1, 4, new Executor(ORA)),
                new InstructionData(0xb2, "ORA D", 1, 4, new Executor(ORA)),
                new InstructionData(0xb3, "ORA E", 1, 4, new Executor(ORA)),
                new InstructionData(0xb4, "ORA H", 1, 4, new Executor(ORA)),
                new InstructionData(0xb5, "ORA L", 1, 4, new Executor(ORA)),
                new InstructionData(0xb6, "ORA M", 1, 7, new Executor(ORA)),
                new InstructionData(0xb7, "ORA A", 1, 4, new Executor(ORA)),
                new InstructionData(0xb8, "CMP B", 1, 4, new Executor(CMP)),
                new InstructionData(0xb9, "CMP C", 1, 4, new Executor(CMP)),
                new InstructionData(0xba, "CMP D", 1, 4, new Executor(CMP)),
                new InstructionData(0xbb, "CMP E", 1, 4, new Executor(CMP)),
                new InstructionData(0xbc, "CMP H", 1, 4, new Executor(CMP)),
                new InstructionData(0xbd, "CMP L", 1, 4, new Executor(CMP)),
                new InstructionData(0xbe, "CMP M", 1, 7, new Executor(CMP)),
                new InstructionData(0xbf, "CMP A", 1, 4, new Executor(CMP)),

                new InstructionData(0xc0, "RNZ", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xc1, "POP B", 1, 10, new Executor(POP)),
                new InstructionData(0xc2, "JNZ ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xc3, "JMP ${0:x4}", 3, 10, new Executor(JMP)),
                new InstructionData(0xc4, "CNZ ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xc5, "PUSH B", 1, 12, new Executor(PUSH)),
                new InstructionData(0xc6, "ADI ${0:x2}", 2, 7, new Executor(ADI)),
                new InstructionData(0xc7, "RST 0", 1, 12, new Executor(RST)),
                new InstructionData(0xc8, "RZ", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xc9, "RET", 1, 10, new Executor(RET)),
                new InstructionData(0xca, "JZ ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xcb, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0xcc, "CZ ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xcd, "CALL ${0:x4}", 3, 18, new Executor(CALL)),
                new InstructionData(0xce, "ACI ${0:x2}", 2, 7, new Executor(ACI)),
                new InstructionData(0xcf, "RST 1", 1, 12, new Executor(RST)),

                new InstructionData(0xd0, "RNC", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xd1, "POP D", 1, 10, new Executor(POP)),
                new InstructionData(0xd2, "JNC ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xd3, "OUT ${0:x2}", 2, 10, new Executor(OUT)),
                new InstructionData(0xd4, "CNC ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xd5, "PUSH D", 1, 12, new Executor(PUSH)),
                new InstructionData(0xd6, "SUI ${0:x2}", 2, 7, new Executor(SUI)),
                new InstructionData(0xd7, "RST 2", 1, 12, new Executor(RST)),
                new InstructionData(0xd8, "RC", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xd9, "Invalid", 1, 10, new Executor(Invalid)),
                new InstructionData(0xda, "JC ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xdb, "IN ${0:x2}", 2, 10, new Executor(IN)),
                new InstructionData(0xdc, "CC ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xdd, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0xde, "SBI ${0:x2}", 2, 7, new Executor(SBI)),
                new InstructionData(0xdf, "RST 3", 1, 12, new Executor(RST)),

                new InstructionData(0xe0, "RPO", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xe1, "POP H", 1, 10, new Executor(POP)),
                new InstructionData(0xe2, "JPO ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xe3, "XTHL", 1, 16, new Executor(XTHL)),
                new InstructionData(0xe4, "CPO ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xe5, "PUSH H", 1, 12, new Executor(PUSH)),
                new InstructionData(0xe6, "ANI ${0:x2}", 2, 7, new Executor(ANI)),
                new InstructionData(0xe7, "RST 4", 1, 12, new Executor(RST)),
                new InstructionData(0xe8, "RPE", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xe9, "PCHL", 1, 6, new Executor(PCHL)),
                new InstructionData(0xea, "JPE ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xeb, "XCHG", 1, 4, new Executor(XCHG)),
                new InstructionData(0xec, "CPE ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xed, "Invalid", 1, 0, new Executor(Invalid)),
                new InstructionData(0xee, "XRI ${0:x2}", 2, 7, new Executor(XRI)),
                new InstructionData(0xef, "RST 5", 1, 12, new Executor(RST)),

                new InstructionData(0xf0, "RP", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xf1, "POP PSW", 1, 10, new Executor(POP)),
                new InstructionData(0xf2, "JP ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xf3, "DI", 1, 4, new Executor(DI)),
                new InstructionData(0xf4, "CP ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xf5, "PUSH PSW", 1, 12, new Executor(PUSH)),
                new InstructionData(0xf6, "ORI ${0:x2}", 2, 7, new Executor(ORI)),
                new InstructionData(0xf7, "RST 6", 1, 12, new Executor(RST)),
                new InstructionData(0xf8, "RM", 1, 12, 6, new Executor(RETC)),
                new InstructionData(0xf9, "SPHL", 1, 6, new Executor(SPHL)),
                new InstructionData(0xfa, "JM ${0:x4}", 3, 10, 7, new Executor(JMP)),
                new InstructionData(0xfb, "EI", 1, 4, new Executor(EI)),
                new InstructionData(0xfc, "CM ${0:x4}", 3, 18, 9, new Executor(CALLC)),
                new InstructionData(0xfd, "Invalid", 3, 0, new Executor(Invalid)),
                new InstructionData(0xfe, "CPI ${0:x2}", 2, 7, new Executor(CPI)),
                new InstructionData(0xff, "RST 7", 1, 12, new Executor(RST)),
            };
        }

        /// <summary>
        /// Big ol' instruction table.  Instruction metadata and jump table.
        /// </summary>
        private InstructionData[] _instructionData;
    }

    
}
