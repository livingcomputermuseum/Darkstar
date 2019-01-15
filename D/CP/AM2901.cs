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
    /// <summary>
    /// This implements an abstraction of the AMD 2901 as seen by the Central Processor --
    /// that is: as a 16-bit ALU + register file, rather than four 4-bit ALUs hooked together.
    /// This only implements the signals and operations that the CP actually cares about.
    /// </summary>
    public class AM2901
    {
        static AM2901()
        {
            BuildTables();
        }

        public AM2901()
        {

        }

        public ushort[] R
        {
            get { return _r; }
        }

        public ushort Q
        {
            get { return _q; }
        }

        /// <summary>
        /// Executes the ALU operation specified by the given microinstruction.
        /// </summary>
        /// <param name="i">The microinstruction</param>
        /// <param name="d">The ALU D input</param>
        /// <param name="carryIn">The ALU Carry in</param>
        /// <param name="loadMAR">Whether this operation is taking place during an MAR<- operation, 
        /// in which case the top half of the ALU needs to be treated specially.</param>
        public ushort Execute(Microinstruction i, ushort d, bool carryIn, bool loadMAR)
        {
            //
            // Save R[a] for the A-bypass case.
            // (If the ALU op ends up modifying R[a] in A-Bypass mode (because a == b)
            // it will happen much later than A-bypass and we want
            // Y to get the original value of R[a], not the later value.)
            //
           
            // Select source data
            int r, s;
            switch(i.aS)
            {
                case AluSourcePair.AQ:
                    r = _r[i.rA];
                    s = _q;
                    break;

                case AluSourcePair.AB:
                    r = _r[i.rA];
                    s = _r[i.rB];
                    break;

                case AluSourcePair.ZQ:
                    r = 0;
                    s = _q;
                    break;

                case AluSourcePair.ZB:
                    r = 0;
                    s = _r[i.rB];
                    break;

                case AluSourcePair.ZA:
                    r = 0;
                    s = _r[i.rA];
                    break;

                case AluSourcePair.DA:
                    r = d;
                    s = _r[i.rA];
                    break;

                case AluSourcePair.DQ:
                    r = d;
                    s = _q;
                    break;

                case AluSourcePair.D0:
                    r = d;
                    s = 0;
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled source pair {0}", i.aS));
            }

            //
            // Do ALU op
            //
            int f;
            int cIn = (carryIn ? 1 : 0);
            switch (i.aF)
            {
                case AluFunction.RplusS:
                    {
                        f = r + s + cIn;
                        CarryOut = (f > 0xffff);
                        NibCarry = (r & 0xf) + (s & 0xf) + cIn > 0xf;
                        PgCarry = (r & 0xff) + (s & 0xff) + cIn > 0xff;
                        int cn = (r & 0xfff) + (s & 0xfff) + cIn > 0xfff ? 1 : 0;
                        Overflow = _overflowTable[r >> 12, s >> 12, cn];
                    }
                    break;

                case AluFunction.SminusR:
                    {
                        f = s + (~r & 0xffff) + cIn;
                        CarryOut = (f > 0xffff);
                        NibCarry = ((~r & 0xf) + (s & 0xf) + cIn > 0xf);
                        PgCarry = ((~r & 0xff) + (s & 0xff) + cIn > 0xff);
                        int cn = (~r & 0xfff) + (s & 0xfff) + cIn > 0xfff ? 1 : 0;
                        Overflow = _overflowTable[(~r & 0xffff) >> 12, s >> 12, cn];
                    }
                    break;

                case AluFunction.RminusS:
                    {
                        f = r + (~s & 0xffff) + cIn;
                        CarryOut = (f > 0xffff);
                        NibCarry = ((r & 0xf) + (~s & 0xf) + cIn > 0xf);
                        PgCarry = ((r & 0xff) + (~s & 0xff) + cIn > 0xff);
                        int cn = (r & 0xfff) + (~s & 0xfff) + cIn > 0xfff ? 1 : 0;
                        Overflow = _overflowTable[r >> 12, (~s & 0xffff) >> 12, cn];
                    }
                    break;

                case AluFunction.RorS:
                    f = r | s;
                    // A few microinstructions do an MAR<- with RorS and expect PgCarry to be set appropriately.
                    NibCarry = _carryTableOr[r & 0xf, s & 0xf, cIn];
                    PgCarry = _carryTableOr[(r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                    CarryOut = false;
                    Overflow = false;
                    break;

                case AluFunction.RandS:
                    f = r & s;
                    NibCarry = false;
                    PgCarry = false;
                    CarryOut = false;
                    Overflow = false;
                    break;

                case AluFunction.notRandS:
                    f = (~r) & s;
                    NibCarry = false;
                    PgCarry = false;
                    CarryOut = false;
                    Overflow = false;
                    break;

                case AluFunction.RxorS:
                    f = r ^ s;
                    NibCarry = false;
                    PgCarry = false;
                    CarryOut = false;
                    Overflow = false;
                    break;

                case AluFunction.notRxorS:
                    f = (~r) ^ s;
                    NibCarry = false;
                    PgCarry = false;
                    CarryOut = false;
                    Overflow = false;
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled function {0}", i.aF));
            }

            // Clip F to 16 bits
            f = f & 0xffff;

            if (loadMAR)
            {
                //
                // If the ALU is being run during a MAR<- operation, the top 8 bits of the ALU are 
                // computed using an operator specified by aF | 3, with the source set to 0,B.
                // The CarryOut and Overflow flags are clear (since they are not affected by the 
                // OR/notXOR operation), and the carry from the least-significant byte of the ALU does not 
                // carry over to the most-significant byte.
                //
                // See page 25 of the microcode ref for details.
                //
                // We implement this here by overwriting the upper byte of F with the upper bits of rB
                // (or its complement).  Interlisp microcode expects Overflow and Carry to be set appropriately.
                //
                switch ((AluFunction)((int)i.aF | 0x3))
                {
                    case AluFunction.RorS:
                        {
                            f = (f & 0xff) | (_r[i.rB] & 0xff00);
                            bool midCarry = _carryTableOr[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                            Overflow = CarryOut = _carryTableOr[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        }
                        break;

                    case AluFunction.notRxorS:
                        {
                            f = (f & 0xff) | ((~_r[i.rB]) & 0xff00);
                            bool midCarry = _carryTableNotXor[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                            CarryOut = _carryTableNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                            Overflow = _overflowNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        }
                        break;
                }
            }
            
            Zero = (f == 0);
            Neg = ((f & 0x8000) != 0);

            //
            // Write outputs, do shifts and cycles as appropriate before writing back.
            // (Shifts and cycles do not affect the Y output, only the register being written back to.)
            //            
            switch (i.AluDestination)
            {
                case 0:
                    _q = (ushort)f;
                    Y = (ushort)f;
                    break;

                case 1:
                    Y = (ushort)f;
                    break;

                case 2:
                    Y = _r[i.rA];
                    _r[i.rB] = (ushort)f;
                    break;

                case 3:
                    _r[i.rB] = (ushort)f;
                    Y = (ushort)f;
                    break;

                case 4:
                    Y = (ushort)f;

                    if (i.Cycle)
                    {
                        // double-word right shift
                        // MSB of Q gets inverted LSB of F.
                        _q = (ushort)((_q >> 1) | ((~f & 0x1) << 15));

                        // MSB of F gets Carry in.
                        f = (ushort)((f >> 1) | (carryIn ? 0x8000 : 0x0));
                    }
                    else
                    {
                        // double-word arithmetic right shift.
                        // MSB of Q gets inverted LSB of F.
                        _q = (ushort)((_q >> 1) | ((~f & 0x1) << 15));

                        // MSB of F gets Carry out.
                        f = (ushort)((f >> 1) | (CarryOut ? 0x8000 : 0x0));
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                case 5:
                    Y = (ushort)f;
                    if (i.Cycle)
                    {
                        // F: single-word right rotate:
                        f = (ushort)((f >> 1) | ((f & 0x1) << 15));                     
                    }
                    else
                    {                        
                        // F: single-word right shift w/carryIn to MSB:
                        f = (ushort)((f >> 1) | (carryIn ? 0x8000 : 0x0));                        
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                case 6:
                    Y = (ushort)f;

                    // double-word left shift (apparently identical for cycle and shift)
                    // LSB of F gets MSB of Q, not inverted.
                    f = (ushort)((f << 1) | ((_q & 0x8000) >> 15));

                    // LSB of Q gets Cin, inverted
                    _q = (ushort)((_q << 1) | (1 - cIn));

                    _r[i.rB] = (ushort)f;                    
                    break;

                case 7:
                    Y = (ushort)f;
                    if (i.Cycle)
                    {
                        // F: single-word left rotate:
                        f = (ushort)((f << 1) | ((f & 0x8000) >> 15));                        
                    }
                    else
                    {
                        // F: single-word left shift w/carryIn to LSB:
                        f = (ushort)((f << 1) | cIn);                        
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled destination {0}", i.aF));
            }

            return Y;
        }

        /// <summary>
        /// Executes the ALU operation specified by the given microinstruction with all condition flags
        /// calculated, even for logical operations.  This is significantly slower than Execute().
        /// </summary>
        /// <param name="i">The microinstruction</param>
        /// <param name="d">The ALU D input</param>
        /// <param name="carryIn">The ALU Carry in</param>
        /// <param name="loadMAR">Whether this operation is taking place during an MAR<- operation, 
        /// in which case the top half of the ALU needs to be treated specially.</param>
        public ushort ExecuteAccurate(Microinstruction i, ushort d, bool carryIn, bool loadMAR)
        {
            // Select source data
            int r, s;
            switch (i.aS)
            {
                case AluSourcePair.AQ:
                    r = _r[i.rA];
                    s = _q;
                    break;

                case AluSourcePair.AB:
                    r = _r[i.rA];
                    s = _r[i.rB];
                    break;

                case AluSourcePair.ZQ:
                    r = 0;
                    s = _q;
                    break;

                case AluSourcePair.ZB:
                    r = 0;
                    s = _r[i.rB];
                    break;

                case AluSourcePair.ZA:
                    r = 0;
                    s = _r[i.rA];
                    break;

                case AluSourcePair.DA:
                    r = d;
                    s = _r[i.rA];
                    break;

                case AluSourcePair.DQ:
                    r = d;
                    s = _q;
                    break;

                case AluSourcePair.D0:
                    r = d;
                    s = 0;
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled source pair {0}", i.aS));
            }

            //
            // Do ALU op
            //
            int f;
            int cIn = (carryIn ? 1 : 0);
            switch (i.aF)
            {
                case AluFunction.RplusS:
                    {
                        f = r + s + cIn;
                        NibCarry = _carryTableArithmetic[r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableArithmetic[(r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableArithmetic[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        CarryOut = _carryTableArithmetic[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        Overflow = _overflowTable[r >> 12, s >> 12, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.SminusR:
                    {
                        f = s + (~r & 0xffff) + cIn;
                        NibCarry = _carryTableArithmetic[~r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableArithmetic[(~r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableArithmetic[(~r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        CarryOut = _carryTableArithmetic[(~r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        Overflow = _overflowTable[(~r & 0xffff) >> 12, s >> 12, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.RminusS:
                    {
                        f = r + (~s & 0xffff) + cIn;
                        NibCarry = _carryTableArithmetic[r & 0xf, ~s & 0xf, cIn];
                        PgCarry = _carryTableArithmetic[(r >> 4) & 0xf, (~s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableArithmetic[(r >> 8) & 0xf, (~s >> 8) & 0xf, PgCarry ? 1 : 0];
                        CarryOut = _carryTableArithmetic[(r >> 12) & 0xf, (~s >> 12) & 0xf, midCarry ? 1 : 0];
                        Overflow = _overflowTable[r >> 12, (~s & 0xffff) >> 12, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.RorS:
                    {
                        f = r | s;

                        NibCarry = _carryTableOr[r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableOr[(r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableOr[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        Overflow = CarryOut = _carryTableOr[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.RandS:
                    {
                        f = r & s;

                        NibCarry = _carryTableAnd[r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableAnd[(r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableAnd[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        Overflow = CarryOut = _carryTableAnd[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.notRandS:
                    {
                        f = (~r) & s;

                        NibCarry = _carryTableAnd[~r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableAnd[(~r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableAnd[(~r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        Overflow = CarryOut = _carryTableAnd[(~r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.RxorS:
                    {
                        f = r ^ s;
                        NibCarry = _carryTableNotXor[~r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableNotXor[(~r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableNotXor[(~r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        CarryOut = _carryTableNotXor[(~r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        Overflow = _overflowNotXor[(~r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                    }
                    break;

                case AluFunction.notRxorS:
                    {
                        f = (~r) ^ s;
                        NibCarry = _carryTableNotXor[r & 0xf, s & 0xf, cIn];
                        PgCarry = _carryTableNotXor[(r >> 4) & 0xf, (s >> 4) & 0xf, NibCarry ? 1 : 0];
                        bool midCarry = _carryTableNotXor[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                        CarryOut = _carryTableNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        Overflow = _overflowNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled function {0}", i.aF));
            }

            // Clip F to 16 bits
            f = f & 0xffff;

            if (loadMAR)
            {
                //
                // If the ALU is being run during a MAR<- operation, the top 8 bits of the ALU are 
                // computed using an operator specified by aF | 3, with the source set to 0,B.
                // The CarryOut and Overflow flags are clear (since they are not affected by the 
                // OR/notXOR operation), and the carry from the least-significant byte of the ALU does not 
                // carry over to the most-significant byte.
                //
                // See page 25 of the microcode ref for details.
                //
                // We implement this here by simply overwriting the upper byte of F with the upper bits of rB
                // (or its complement), and clearing CarryOut and Overflow.
                //
                switch ((AluFunction)((int)i.aF | 0x3))
                {
                    case AluFunction.RorS:
                        {
                            f = (f & 0xff) | (_r[i.rB] & 0xff00);
                            bool midCarry = _carryTableOr[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                            Overflow = CarryOut = _carryTableOr[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        }
                        break;

                    case AluFunction.notRxorS:
                        {
                            f = (f & 0xff) | ((~_r[i.rB]) & 0xff00);
                            bool midCarry = _carryTableNotXor[(r >> 8) & 0xf, (s >> 8) & 0xf, PgCarry ? 1 : 0];
                            CarryOut = _carryTableNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                            Overflow = _overflowNotXor[(r >> 12) & 0xf, (s >> 12) & 0xf, midCarry ? 1 : 0];
                        }
                        break;
                }
            }

            Zero = (f == 0);
            Neg = ((f & 0x8000) != 0);

            //
            // Write outputs, do shifts and cycles as appropriate before writing back.
            // (Shifts and cycles do not affect the Y output, only the register being written back to.)
            //            
            switch (i.AluDestination)
            {
                case 0:
                    _q = (ushort)f;
                    Y = (ushort)f;
                    break;

                case 1:
                    Y = (ushort)f;
                    break;

                case 2:
                    Y = _r[i.rA];
                    _r[i.rB] = (ushort)f;                    
                    break;

                case 3:
                    _r[i.rB] = (ushort)f;
                    Y = (ushort)f;
                    break;

                case 4:
                    Y = (ushort)f;

                    if (i.Cycle)
                    {
                        // double-word right shift
                        // MSB of Q gets inverted LSB of F.
                        _q = (ushort)((_q >> 1) | ((~f & 0x1) << 15));

                        // MSB of F gets Carry in.
                        f = (ushort)((f >> 1) | (carryIn ? 0x8000 : 0x0));
                    }
                    else
                    {
                        // double-word arithmetic right shift.
                        // MSB of Q gets inverted LSB of F.
                        _q = (ushort)((_q >> 1) | ((~f & 0x1) << 15));

                        // MSB of F gets Carry out.
                        f = (ushort)((f >> 1) | (CarryOut ? 0x8000 : 0x0));
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                case 5:
                    Y = (ushort)f;
                    if (i.Cycle)
                    {
                        // F: single-word right rotate:
                        f = (ushort)((f >> 1) | ((f & 0x1) << 15));
                    }
                    else
                    {
                        // F: single-word right shift w/carryIn to MSB:
                        f = (ushort)((f >> 1) | (carryIn ? 0x8000 : 0x0));
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                case 6:
                    Y = (ushort)f;

                    // double-word left shift (apparently identical for cycle and shift)
                    // LSB of F gets MSB of Q, not inverted.
                    f = (ushort)((f << 1) | ((_q & 0x8000) >> 15));

                    // LSB of Q gets Cin, inverted
                    _q = (ushort)((_q << 1) | (1 - cIn));

                    _r[i.rB] = (ushort)f;
                    break;

                case 7:
                    Y = (ushort)f;
                    if (i.Cycle)
                    {
                        // F: single-word left rotate:
                        f = (ushort)((f << 1) | ((f & 0x8000) >> 15));
                    }
                    else
                    {
                        // F: single-word left shift w/carryIn to LSB:
                        f = (ushort)((f << 1) | cIn);
                    }
                    _r[i.rB] = (ushort)f;
                    break;

                default:
                    throw new InvalidOperationException(
                        String.Format("Unhandled destination {0}", i.aF));
            }

            return Y;
        }


        /// <summary>
        /// TODO: replace with a nice table lookup.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="cIn"></param>
        /// <returns></returns>
        private static bool CalcOverflow(int r, int s, int cIn)
        {
            int p0 = (r | s) & 0x1;
            int p1 = ((r | s) & 0x2) >> 1;
            int p2 = ((r | s) & 0x4) >> 2;
            int p3 = ((r | s) & 0x8) >> 3;            

            int g0 = (r & s & 0x1);
            int g1 = (r & s & 0x2) >> 1;
            int g2 = (r & s & 0x4) >> 2;
            int g3 = (r & s & 0x8) >> 3;

            int c4 = g3 | (p3 & g2) | (p3 & p2 & g1) | (p3 & p2 & p1 & g0) | (p3 & p2 & p1 & p0 & cIn);
            int c3 = g2 | (p2 & g1) | (p2 & p1 & g0) | (p2 & p1 & p0 & cIn);

            return (c3 ^ c4) != 0;
        }

        private static bool CalcCarryArithmetic(int r, int s, int cIn)
        {
            int p0 = (r | s) & 0x1;
            int p1 = ((r | s) & 0x2) >> 1;
            int p2 = ((r | s) & 0x4) >> 2;
            int p3 = ((r | s) & 0x8) >> 3;

            int g0 = (r & s & 0x1);
            int g1 = (r & s & 0x2) >> 1;
            int g2 = (r & s & 0x4) >> 2;
            int g3 = (r & s & 0x8) >> 3;

            int c4 = g3 | (p3 & g2) | (p3 & p2 & g1) | (p3 & p2 & p1 & g0) | (p3 & p2 & p1 & p0 & cIn);

            return c4 != 0;
        }

        private static bool CalcCarryOr(int r, int s, int cIn)
        {
            int p0 = (r | s) & 0x1;
            int p1 = ((r | s) & 0x2) >> 1;
            int p2 = ((r | s) & 0x4) >> 2;
            int p3 = ((r | s) & 0x8) >> 3;

            int c4 = (~(p3 & p2 & p1 & p0) & 0x1) | cIn;

            return c4 != 0;
        }

        private static bool CalcCarryAnd(int r, int s, int cIn)
        {
            int g0 = (r & s & 0x1);
            int g1 = (r & s & 0x2) >> 1;
            int g2 = (r & s & 0x4) >> 2;
            int g3 = (r & s & 0x8) >> 3;

            int c4 = g3 | g2 | g1 | g0 | cIn;

            return c4 != 0;
        }

        private static bool CalcCarryNotXor(int r, int s, int cIn)
        {
            int p0 = (r | s) & 0x1;
            int p1 = ((r | s) & 0x2) >> 1;
            int p2 = ((r | s) & 0x4) >> 2;
            int p3 = ((r | s) & 0x8) >> 3;

            int g0 = (r & s & 0x1);
            int g1 = (r & s & 0x2) >> 1;
            int g2 = (r & s & 0x4) >> 2;
            int g3 = (r & s & 0x8) >> 3;

            int c4 = ~(g3 | (p3 & g2) | (p3 & p2 & g1) | (p3 & p2 & p1 & p0 & (g0 | ~cIn))) & 0x1;

            return c4 != 0;
        }

        private static bool CalcOverflowNotXor(int r, int s, int cIn)
        {
            int p0 = (r | s) & 0x1;
            int p1 = ((r | s) & 0x2) >> 1;
            int p2 = ((r | s) & 0x4) >> 2;
            int p3 = ((r | s) & 0x8) >> 3;

            int g0 = (r & s & 0x1);
            int g1 = (r & s & 0x2) >> 1;
            int g2 = (r & s & 0x4) >> 2;
            int g3 = (r & s & 0x8) >> 3;

            int ovr = ((~p2 | (~g2 & ~p1) | (~g2 & ~g1 & ~p0) | (~g2 & ~g1 & ~g0 & cIn)) ^
                (~p3 | (~g3 & ~p2) | (~g3 & ~g2 & ~p1) | (~g3 & ~g2 & ~g1 & ~p0) | (~g3 & ~g2 & ~g1 & ~g0 & cIn))) & 0x1;

            return ovr != 0;
        }

        private static void BuildTables()
        {
            for (int r = 0; r < 16; r++)
            {
                for (int s = 0; s < 16; s++)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        _overflowTable[r, s, c] = CalcOverflow(r, s, c);
                        _carryTableArithmetic[r, s, c] = CalcCarryArithmetic(r, s, c);
                        _carryTableOr[r, s, c] = CalcCarryOr(r, s, c);
                        _carryTableAnd[r, s, c] = CalcCarryAnd(r, s, c);
                        _carryTableNotXor[r, s, c] = CalcCarryNotXor(r, s, c);
                        _overflowNotXor[r, s, c] = CalcOverflowNotXor(r, s, c);
                    }
                }
            }
        }

        //
        // Overflow lookup table for most-significant nibble.
        //
        private static bool[,,] _overflowTable = new bool[16, 16, 2];
        private static bool[,,] _carryTableArithmetic = new bool[16, 16, 2];
        private static bool[,,] _carryTableOr = new bool[16, 16, 2];
        private static bool[,,] _carryTableAnd = new bool[16, 16, 2];
        private static bool[,,] _carryTableNotXor = new bool[16, 16, 2];
        private static bool[,,] _overflowNotXor = new bool[16, 16, 2];

        //
        // Registers
        //
        private ushort[] _r = new ushort[16];
        private ushort _q;

        //
        // Flags
        //
        public bool Zero;
        public bool Neg;
        public bool NibCarry;
        public bool PgCarry;
        public bool CarryOut;
        public bool Overflow;

        //
        // Output
        //
        public ushort Y;
    }
}
