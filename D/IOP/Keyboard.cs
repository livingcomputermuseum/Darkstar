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
using System.Collections.Generic;
using System.Threading;

namespace D.IOP
{
    /// <summary>
    /// Defines the scancodes understood by the IOP.    
    /// The below is currently derived from work undertaken by Al Kossow
    /// (many thanks.)
    /// This table corresponds to the standard US keyboard layout for the
    /// Star/1108.  TODO: How to deal with international arrangements?
    /// </summary>
    public enum KeyCode
    {
        Invalid = 0x00,

        D1 = 0x10,
        T10 = 0x11,
        Defaults = 0x12,    // T9
        LargerSmaller = 0x13, // T8
        Subscript = 0x14,   // T7
        Undo = 0x15,        // R6
        Superscript = 0x16, // T6
        Properties = 0x17,  // L12  (Also Ctrl)
        Move = 0x18,        // L9
        Copy = 0x19,        // L6
        Underline = 0x1a,   // T5
        Italics = 0x1b,     // T4
        Bold = 0x1c,        // T3
        Center = 0x1d,      // T2
        T1 = 0x1e,

        R4 = 0x20,
        SkipNext = 0x22,    // R1
        Help = 0x23,        // R2
        Margins = 0x24,     // R5
        R3 = 0x25,
        L10 = 0x27,
        Same = 0x28,        // L7
        L4 = 0x29,
        L1 = 0x2a,
        A9 = 0x2d,

        DefnExpand = 0x30,  // R7 (Also Esc)
        R10 = 0x31,
        Keyboard = 0x32,    // R11   (^X)
        Font = 0x33,        // R8    (\,|)
        R9 = 0x34,
        Stop = 0x35,        // R12
        Space = 0x36,       
        Open = 0x37,        // L11 (Also Meta)
        L8 = 0x38,
        Find = 0x39,        // L5
        Again = 0x3a,       // L2
        Delete = 0x3b,      // L3
        A8 = 0x3c,
        A11 = 0x3d,

        A12 = 0x41,         // Shift ?
        RightShift = 0x42,      // A6
        FSlash = 0x43,
        Period = 0x44,
        Comma = 0x45,
        M = 0x46,
        N = 0x47,
        B = 0x48,
        V = 0x49,
        C = 0x4a,
        X = 0x4b,
        Z = 0x4c,
        K47 = 0x4e,

        Return = 0x50,      // A4
        BackQuote = 0x51,   // K46
        Quote = 0x52,       // K43
        Colon = 0x53,
        L = 0x54,
        K = 0x55,
        J = 0x56,
        H = 0x57,
        G = 0x58,
        F = 0x59,
        D = 0x5a,
        S = 0x5b,
        A = 0x5c,
        Lock = 0x5e,        // A3
        LeftShift = 0x5f,   // A5

        A10 = 0x60,
        RBracket = 0x61,    // K45
        LBracket = 0x62,    // K42
        P = 0x63,
        O = 0x64,
        I = 0x65,
        U = 0x66,
        Y = 0x67,
        T = 0x68,
        R = 0x69,
        E = 0x6a,
        W = 0x6b,
        Q = 0x6c,
        Tab = 0x6d,          // A1
        D2 = 0x6f,

        Backspace = 0x70,    // A2
        Equals = 0x71,
        Minus = 0x72,
        N0 = 0x73,
        N9 = 0x74,
        N8 = 0x75,
        N7 = 0x76,
        N6 = 0x77,
        N5 = 0x78,
        N4 = 0x79,
        N3 = 0x7a,
        N2 = 0x7b,
        N1 = 0x7c,
        FArrow = 0x7d,      // K48

    }

    public class Keyboard
    {
        public Keyboard()
        {
            _keyboardQueue = new Queue<KeyCode>();

            _lock = new ReaderWriterLockSlim();
        }

        public byte ReadData()
        {
            if (Log.Enabled) Log.Write(LogComponent.IOPKeyboard, "Key data {0} (0x{1:x2}) read.", _keyData, (int)_keyData);
            return (byte)_keyData;
        }

        public void NextData()
        {
            _lock.EnterUpgradeableReadLock();
            if (_keyboardQueue.Count > 0)
            {
                _lock.EnterWriteLock();
                _keyData = _keyboardQueue.Dequeue();
                if (Log.Enabled) Log.Write(LogComponent.IOPKeyboard, "Key data {0} (0x{1:x2}) dequeued.", _keyData, (int)_keyData);
                _lock.ExitWriteLock();
            }
            else
            {
                // No data available.
                _keyData = KeyCode.Invalid;
            }
            _lock.ExitUpgradeableReadLock();
        }

        public bool DataReady()
        {
            _lock.EnterReadLock();
            bool ready = _keyboardQueue.Count > 0;
            _lock.ExitReadLock();
            return ready;
        }

        public void EnableDiagnosticMode()
        {
            //
            // Per MoonIOPCSTest.asm:
            // "Return sequence of events should be all characters held down followed by D2, D1."
            // Right now, just enqueue d2, d1

            _lock.EnterWriteLock();

            _keyboardQueue.Enqueue(KeyCode.D2);
            _keyboardQueue.Enqueue(KeyCode.D1);

            _lock.ExitWriteLock();
        }

        public void DisableDiagnosticMode()
        {
            _lock.EnterWriteLock();
            _keyboardQueue.Clear();
            _lock.ExitWriteLock();
        }

        public void KeyDown(KeyCode keycode)
        {
            _lock.EnterWriteLock();
            // Bit 0 = 0 indicates the key being pressed
            _keyboardQueue.Enqueue((KeyCode)((int)keycode & 0x7f));
            _lock.ExitWriteLock();
        }

        public void KeyUp(KeyCode keycode)
        {
            _lock.EnterWriteLock();
            // Bit 0 = 1 indicates the key being released
            _keyboardQueue.Enqueue((KeyCode)((int)keycode | 0x80));
            _lock.ExitWriteLock();
        }

        private KeyCode _keyData;
        private Queue<KeyCode> _keyboardQueue;

        private ReaderWriterLockSlim _lock;

    }
}
