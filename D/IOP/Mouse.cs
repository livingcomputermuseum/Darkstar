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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.IOP
{
    [Flags]
    public enum StarMouseButton
    {
        None = 0,
        Left = 0x4,
        Right = 0x2,
        Middle = 0x1,        
    }

    public class Mouse
    {
        public Mouse()
        {

        }

        public int MouseX
        {
            get { return _mouseX; }
        }

        public int MouseY
        {
            get { return _mouseY; }
        }

        public StarMouseButton Buttons
        {
            get { return _buttons; }
        }

        public void Clear()
        {
            _mouseX = 0;
            _mouseY = 0;
        }

        public void MouseDown(StarMouseButton button)
        {
            _buttons |= button;
        }

        public void MouseUp(StarMouseButton button)
        {
            _buttons &= (~button);
        }

        public void MouseMove(int dx, int dy)
        {
            _mouseX += dx;
            _mouseY += dy;

            // Clip into range (-128 to 127)
            _mouseX = Math.Max(-128, _mouseX);
            _mouseX = Math.Min(127, _mouseX);

            _mouseY = Math.Max(-128, _mouseY);
            _mouseY = Math.Min(127, _mouseY);
        }

        private int _mouseX;
        private int _mouseY;

        private StarMouseButton _buttons;
    }
}
