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
using SDL2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace D.UI
{
    /// <summary>
    /// This portion of the DWindow class implements methods related to 
    /// providing the emulator's display and keyboard/mouse inputs.
    /// 
    /// At the moment it uses SDL for display and mouse, but WinForms for keyboard -- WinForms
    /// appears to swallow all keyboard events in its message loop whether they're handled or not,
    /// so SDL never sees them.
    /// It would be preferable to get SDL to handle all of this, and leave WinForms to manage only
    /// the user-interface portions.  (Really, it would be preferable to use a cross-platform
    /// UI library that has decent C# wrappers but I've yet to find one that actually works.
    /// WinForms at least nominally works on *nix/OS X platforms under mono though it is quite buggy on
    /// OS X.)
    /// </summary>
    public partial class DWindow : Form
    {
        private void InitializeIO()
        {          
            UpdateDisplayScale();
            UpdateSlowPhosphor();

            InitializeKeymapSDL();
            InitializeKeymapWinforms();

            InitializeSDL();

            _currentCursorState = true;
            _mouseCaptured = false;
            _skipNextMouseMove = false;

            _capsLock = false;
        }

        /// <summary>
        /// Renders a full scanline of pixels to the current frame.
        /// </summary>
        /// <param name="scanline"></param>
        /// <param name="scanlineData"></param>
        /// <param name="invert"></param>
        public void DrawScanline(int scanline, ushort[] scanlineData, bool invert)
        {
            int rgbIndex = scanline * _displayWidth;
            uint onColor;
            uint offColor;
            if (invert)
            {
                onColor = _litPixel;
                offColor = _offPixel;
            }
            else
            {
                offColor = _litPixel;
                onColor = _offPixel;
            }

            for (int i = 0; i < scanlineData.Length; i++)
            {
                ushort w = scanlineData[i];
                for (int bit = 15; bit >= 0; bit--)
                {
                    uint color = (w & (1 << bit)) == 0 ? offColor : onColor;
                    _32bppDisplayBuffer[rgbIndex++] = (int)(color);
                }
            }
        }

        public void Clear()
        {
            uint blankColor = 0xff000000;
            for(int i=0;i<_32bppDisplayBuffer.Length;i++)
            {
                _32bppDisplayBuffer[i] = (int)blankColor;
            }
            
            Render();
        }

        /// <summary>
        /// Renders a completed frame to the screen.
        /// </summary>
        public void Render()
        {
            //
            // Send a render event to the SDL message loop so that things
            // will get rendered.
            //            
            BeginInvoke(new DisplayDelegate(RenderDisplay));

            _frameCount++;
        }

        protected override void OnLoad(EventArgs e)
        {
            //
            // Kick off our SDL message loop.
            //
            _sdlThread = new Thread(SDLMessageLoopThread);
            _sdlThread.Start();

            // Clear the display.
            Clear();

            base.OnLoad(e);
        }

        private void RenderDisplay()
        {
            //
            // Stuff the display data into the display texture
            //
            IntPtr textureBits = IntPtr.Zero;
            int pitch = 0;
            SDL.SDL_LockTexture(_displayTexture, IntPtr.Zero, out textureBits, out pitch);

            Marshal.Copy(_32bppDisplayBuffer, 0, textureBits, _32bppDisplayBuffer.Length);

            SDL.SDL_UnlockTexture(_displayTexture);

            //
            // Render the display texture to the renderer
            //
            SDL.SDL_RenderCopy(_sdlRenderer, _displayTexture, IntPtr.Zero, IntPtr.Zero);

            //
            // And show it to us.
            //
            SDL.SDL_RenderPresent(_sdlRenderer);
        }

        private void SDLMessageLoopThread()
        {
            _sdlRunning = true;

            while (_sdlRunning)
            {
                SDL.SDL_Event e;

                //
                // Run main message loop
                //
                while (SDL.SDL_WaitEvent(out e) != 0)
                {
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        _sdlRunning = false;
                        break;
                    }
                    else
                    {
                        //
                        // Ensure things get run on the UI thread.
                        //
                        Invoke(new SDLMessageHandlerDelegate(SDLMessageHandler), e);
                    }
                }

                SDL.SDL_Delay(0);
            }

            //
            // Shut things down nicely.
            //
            if (_sdlRenderer != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_sdlRenderer);
                _sdlRenderer = IntPtr.Zero;
            }

            //
            // We only destroy the window and call SDL_Quit() on Windows systems 
            // when cleaning up.  
            // Doing so on *nix results in SDL_DestroyWindow and SDL_Quit() hanging forever.  
            // At least on my Debian box.  Why?  NO IDEA.  Platform independence
            // is a a nightmare I shall never awaken from.
            //
            if (Configuration.Platform == PlatformType.Windows)
            {
                if (_sdlWindow != IntPtr.Zero)
                {
                    SDL.SDL_DestroyWindow(_sdlWindow);
                    _sdlWindow = IntPtr.Zero;             
                }

                SDL.SDL_Quit();
            }
        }

        private void SDLMessageHandler(SDL.SDL_Event e)
        {
            //
            // Handle current messages.  This executes in the UI context.
            //            
            switch (e.type)
            {                
                case SDL.SDL_EventType.SDL_USEREVENT:
                    // This should always be the case since we only define one
                    // user event, but just to be truly pedantic...
                    if (e.user.type == _renderEventType)
                    {
                        RenderDisplay();
                    }
                    break;

                    /*
                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    DoMouseMove(e.motion.x, e.motion.y);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    DoMouseDown(e.button.button, e.button.x, e.button.y);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    DoMouseUp(e.button.button);
                    break; */

                /*
                 * These do not currently function due to WinForms's 
                 * message loop getting in the way.
                 * 
                case SDL.SDL_EventType.SDL_KEYDOWN:                    
                    SdlKeyDown(e.key.keysym.sym);
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:                    
                    SdlKeyUp(e.key.keysym.sym);
                    break;
                */
                default:
                    break;
            }

        }

        private void SdlKeyDown(SDL.SDL_Keycode key)
        {
            //
            // Check for Alt key to release mouse
            //
            if (key == SDL.SDL_Keycode.SDLK_LALT ||
                key == SDL.SDL_Keycode.SDLK_RALT)
            {
                ReleaseMouse();
            }

            if (!_mouseCaptured)
            {
                return;
            }

            if (_sdlKeyMap.ContainsKey(key))
            {
                KeyCode code = _sdlKeyMap[key];

                if (code == KeyCode.Lock)
                {
                    if (!_capsLock)
                    {
                        // Latch Lock
                        _system.IOP.Keyboard.KeyDown(code);
                    }
                    else
                    {
                        // Release
                        _system.IOP.Keyboard.KeyUp(code);
                    }

                    _capsLock = !_capsLock;
                }
                else
                {
                    _system.IOP.Keyboard.KeyDown(code);
                }
            }
        }

        private void SdlKeyUp(SDL.SDL_Keycode key)
        {
            if (!_mouseCaptured)
            {
                return;
            }

            if (_sdlKeyMap.ContainsKey(key))
            {
                KeyCode code = _sdlKeyMap[key];

                if (code != KeyCode.Lock)
                {
                    _system.IOP.Keyboard.KeyUp(code);
                }
            }
        }

        /// <summary>
        /// Handle modifier keys here mostly because Windows Forms doesn't 
        /// normally distinguish between left and right Shift or left and
        /// right Control keys.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected override bool ProcessKeyEventArgs(ref Message m)
        {            
            bool extended = (m.LParam.ToInt64() & 0x1000000) != 0;
            bool down = false;

            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            

            if (m.Msg == WM_KEYDOWN)
            {
                down = true;
            }
            else if (m.Msg == WM_KEYUP)
            {
                down = false;
            }
            else
            {
                // Something else?
                return base.ProcessKeyEventArgs(ref m);
            }

            KeyCode modifierKey = KeyCode.Invalid;

            if (Configuration.Platform == PlatformType.Windows)
            {
                switch (m.WParam.ToInt64())
                {
                    case 0x10:
                        // Shift
                        modifierKey = extended ? KeyCode.RightShift : KeyCode.LeftShift;
                        break;

                    case 0x11:
                        // Control
                        modifierKey = extended ? KeyCode.Open : KeyCode.Properties;
                        break;
                }
            }
            else
            {
                //
                // This is a workaround for an apparent bug in Mono's Winforms implementation:
                // The extended bit for RShift and RControl isn't set properly -- for Control
                // it inexplicably gets set on WM_KEYUP, for Shift it never gets set at all.
                // Instead we look at the scancode coming in the lparam -- this is implementation
                // dependent so it probably isn't a great idea and may be fragile.
                //
                switch ((m.LParam.ToInt64() >> 16) & 0xff)
                {
                    case 0x25:
                        modifierKey = KeyCode.Properties;
                        break;

                    case 0x69:
                        modifierKey = KeyCode.Open;
                        break;

                    case 0x32:
                        modifierKey = KeyCode.LeftShift;
                        break;

                    case 0x3e:
                        modifierKey = KeyCode.RightShift;
                        break;
                }
            }

            if (modifierKey != KeyCode.Invalid)
            {
                if (down)
                {
                    _system.IOP.Keyboard.KeyDown(modifierKey);
                }
                else
                {
                    _system.IOP.Keyboard.KeyUp(modifierKey);
                }

                return true; // handled
            }

            return base.ProcessKeyEventArgs(ref m);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Handle non-modifier keys here
            if (_winKeyMap.ContainsKey(e.KeyCode))
            {
                //
                // Handle the "Lock" (Caps Lock) key specially -- the real keyboard
                // has a latch that holds it down the first time it is pressed, and
                // releases it the second time.  We simulate that behavior here.
                //
                KeyCode code = _winKeyMap[e.KeyCode];

                if (code == KeyCode.Lock)
                {
                    if (!_capsLock)
                    {
                        // Latch Lock                        
                        _system.IOP.Keyboard.KeyDown(code);
                    }
                    else
                    {
                        // Release
                        _system.IOP.Keyboard.KeyUp(code);
                    }

                    _capsLock = !_capsLock;
                }
                else
                {
                    _system.IOP.Keyboard.KeyDown(code);
                }
            }

            if (e.Alt)
            {
                ReleaseMouse();
                e.SuppressKeyPress = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // Handle non-modifier keys here
            if (_winKeyMap.ContainsKey(e.KeyCode))
            {
                KeyCode code = _winKeyMap[e.KeyCode];

                if (code != KeyCode.Lock)
                {
                    _system.IOP.Keyboard.KeyUp(code);
                }
            }

            if (e.Alt)
            {
                ReleaseMouse();
                e.SuppressKeyPress = true;
            }
        }

        private void OnWinformsMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseCaptured)
            {
                return;
            }

            if (_skipNextMouseMove)
            {
                _skipNextMouseMove = false;
                return;
            }

            //
            // Calclate the center of the window.
            //
            Point middle = new Point(DisplayBox.Width / 2, DisplayBox.Height / 2);

            int dx = e.X - middle.X;
            int dy = e.Y - middle.Y;

            if (dx != 0 || dy != 0)
            {
                _system.IOP.Mouse.MouseMove(dx, dy);

                // Don't handle the very next Mouse Move event (which will just be the motion we caused in the
                // below line...)
                _skipNextMouseMove = true;

                Cursor.Position = DisplayBox.PointToScreen(middle);
            }
        }

        private void DoMouseMove(int x, int y)
        {            
            if (!_mouseCaptured)
            {
                return;
            }

            if (_skipNextMouseMove)
            {
                _skipNextMouseMove = false;
                return;
            }

            //
            // Calclate the center of the window.
            //
            int mx = DisplayBox.Width / 2;
            int my = DisplayBox.Height / 2;

            int dx = x - mx;
            int dy = y - my;

            if (dx != 0 || dy != 0)
            {
                _system.IOP.Mouse.MouseMove(dx, dy);

                // Don't handle the very next Mouse Move event (which will just be the motion we caused in the
                // below line...)
                _skipNextMouseMove = true;

                //
                // Move the mouse pointer to the middle of the window.
                //
                SDL.SDL_WarpMouseInWindow(_sdlWindow, mx, my);
            }
        }

        private void OnWinformsMouseDown(object sender, MouseEventArgs e)
        {            
            if (!_mouseCaptured)
            {
                return;
            }

            StarMouseButton starButton = GetMouseButtonWinforms(e.Button);

            if (starButton != StarMouseButton.None)
            {
                _system.IOP.Mouse.MouseDown(starButton);
            }
        }

        private void DoMouseDown(byte button, int x, int y)
        {
            //
            // OS X Sierra issue: we get mousedown events when the window title
            // is clicked, sometimes.  These always show up with a Y coordinate
            // of zero.  So ignore those only for mouse-capture purposes as
            // a workaround.
            //
            if (!_mouseCaptured && (x <= 0 || y <= 0))
            {
                return;
            }

            if (!_mouseCaptured)
            {

                return;
            }

            StarMouseButton starButton = GetMouseButtonSDL(button);

            if (starButton != StarMouseButton.None)
            {
                _system.IOP.Mouse.MouseDown(starButton);
            }
        }

        private void OnWinformsMouseUp(object sender, MouseEventArgs e)
        {            
            if (!_mouseCaptured)
            {
                CaptureMouse();
                return;
            }

            StarMouseButton starButton = GetMouseButtonWinforms(e.Button);

            if (starButton != StarMouseButton.None)
            {
                _system.IOP.Mouse.MouseUp(starButton);
            }
        }

        private void DoMouseUp(byte button)
        {
            if (!_mouseCaptured)
            {
                CaptureMouse();
                return;
            }

            StarMouseButton starButton = GetMouseButtonSDL(button);

            if (starButton != StarMouseButton.None)
            {
                _system.IOP.Mouse.MouseUp(starButton);
            }
        }

        private void CaptureMouse()
        {
            //
            // Turn off the mouse cursor (both for SDL and WinForms) and ensure the mouse is trapped
            // within our window.
            //
            if (_system.IsExecuting)
            {
                _mouseCaptured = true;
                SDL.SDL_ShowCursor(0);
                ShowCursor(false);
                SDL.SDL_SetWindowGrab(_sdlWindow, SDL.SDL_bool.SDL_TRUE);
            }

            UpdateMouseState();
        }

        private void ReleaseMouse()
        {
            //
            // Turn the mouse cursor back on (both for SDL and WinForms), and release the mouse.
            //
            _mouseCaptured = false;
            SDL.SDL_ShowCursor(1);
            ShowCursor(true);
            SDL.SDL_SetWindowGrab(_sdlWindow, SDL.SDL_bool.SDL_FALSE);

            UpdateMouseState();
        }


        private void OnWindowLeave(object sender, EventArgs e)
        {
            // We are no longer the focus, make sure to release the mouse.
            ReleaseMouse();
        }

        private void OnWindowDeactivate(object sender, EventArgs e)
        {
            // We are no longer the focus, make sure to release the mouse.
            ReleaseMouse();
        }

        /// <summary>
        /// This works around Winforms's ref-counted cursor behavior.
        /// </summary>
        /// <param name="show"></param>
        private void ShowCursor(bool show)
        {
            if (show == _currentCursorState)
            {
                return;
            }

            if (show)
            {
                Cursor.Show();
            }
            else
            {
                Cursor.Hide();
            }

            _currentCursorState = show;
        }

        private void UpdateDisplayScale()
        {
            _displayScale = Configuration.DisplayScale;
            //
            // Force the UIPanel (which holds the Star's display plus the status bar)
            // to be the dimensions of the (possibly scaled) display + status bar.
            // This will cause the window to resize to fit.
            //
            UIPanel.Size =
                new Size(
                    (int)(_displayWidth * _displayScale),
                    (int)(_displayHeight * _displayScale) + this.SystemStatus.Height);
        }

        private void UpdateSlowPhosphor()
        {
            if (Configuration.SlowPhosphor)
            {
                _litPixel = _litPixelSlow;
                _offPixel = _offPixelSlow;
            }
            else
            {
                _litPixel = _litPixelNormal;
                _offPixel = _offPixelNormal;
            }
        }

        private void InitializeKeymapWinforms()
        {
            _winKeyMap = new Dictionary<Keys, KeyCode>();

            _winKeyMap.Add(Keys.A, KeyCode.A);
            _winKeyMap.Add(Keys.B, KeyCode.B);
            _winKeyMap.Add(Keys.C, KeyCode.C);
            _winKeyMap.Add(Keys.D, KeyCode.D);
            _winKeyMap.Add(Keys.E, KeyCode.E);
            _winKeyMap.Add(Keys.F, KeyCode.F);
            _winKeyMap.Add(Keys.G, KeyCode.G);
            _winKeyMap.Add(Keys.H, KeyCode.H);
            _winKeyMap.Add(Keys.I, KeyCode.I);
            _winKeyMap.Add(Keys.J, KeyCode.J);
            _winKeyMap.Add(Keys.K, KeyCode.K);
            _winKeyMap.Add(Keys.L, KeyCode.L);
            _winKeyMap.Add(Keys.M, KeyCode.M);
            _winKeyMap.Add(Keys.N, KeyCode.N);
            _winKeyMap.Add(Keys.O, KeyCode.O);
            _winKeyMap.Add(Keys.P, KeyCode.P);
            _winKeyMap.Add(Keys.Q, KeyCode.Q);
            _winKeyMap.Add(Keys.R, KeyCode.R);
            _winKeyMap.Add(Keys.S, KeyCode.S);
            _winKeyMap.Add(Keys.T, KeyCode.T);
            _winKeyMap.Add(Keys.U, KeyCode.U);
            _winKeyMap.Add(Keys.V, KeyCode.V);
            _winKeyMap.Add(Keys.W, KeyCode.W);
            _winKeyMap.Add(Keys.X, KeyCode.X);
            _winKeyMap.Add(Keys.Y, KeyCode.Y);
            _winKeyMap.Add(Keys.Z, KeyCode.Z);

            _winKeyMap.Add(Keys.D0, KeyCode.N0);
            _winKeyMap.Add(Keys.D1, KeyCode.N1);
            _winKeyMap.Add(Keys.D2, KeyCode.N2);
            _winKeyMap.Add(Keys.D3, KeyCode.N3);
            _winKeyMap.Add(Keys.D4, KeyCode.N4);
            _winKeyMap.Add(Keys.D5, KeyCode.N5);
            _winKeyMap.Add(Keys.D6, KeyCode.N6);
            _winKeyMap.Add(Keys.D7, KeyCode.N7);
            _winKeyMap.Add(Keys.D8, KeyCode.N8);
            _winKeyMap.Add(Keys.D9, KeyCode.N9);

            _winKeyMap.Add(Keys.OemMinus, KeyCode.Minus);
            _winKeyMap.Add(Keys.Oemplus, KeyCode.Equals);

            _winKeyMap.Add(Keys.Return, KeyCode.Return);
            _winKeyMap.Add(Keys.Space, KeyCode.Space);
            _winKeyMap.Add(Keys.Back, KeyCode.Backspace);
            _winKeyMap.Add(Keys.OemOpenBrackets, KeyCode.LBracket);
            _winKeyMap.Add(Keys.OemCloseBrackets, KeyCode.RBracket);
            _winKeyMap.Add(Keys.OemPeriod, KeyCode.Period);
            _winKeyMap.Add(Keys.Oemcomma, KeyCode.Comma);
            _winKeyMap.Add(Keys.OemQuestion, KeyCode.FSlash);
            _winKeyMap.Add(Keys.OemSemicolon, KeyCode.Colon);
            _winKeyMap.Add(Keys.OemQuotes, KeyCode.Quote);
            _winKeyMap.Add(Keys.Oemtilde, KeyCode.BackQuote);
            _winKeyMap.Add(Keys.Right, KeyCode.FArrow);
            _winKeyMap.Add(Keys.Tab, KeyCode.Tab);
            _winKeyMap.Add(Keys.CapsLock, KeyCode.Lock);

            // Left key block (Open/Properties are also mapped to the Ctrl keys
            // as they are used as Meta/Ctrl in interlisp)
            _winKeyMap.Add(Keys.F1, KeyCode.Again);
            _winKeyMap.Add(Keys.F2, KeyCode.Delete);
            _winKeyMap.Add(Keys.F3, KeyCode.Find);
            _winKeyMap.Add(Keys.F4, KeyCode.Copy);
            _winKeyMap.Add(Keys.F5, KeyCode.Same);
            _winKeyMap.Add(Keys.F6, KeyCode.Move);
            _winKeyMap.Add(Keys.F7, KeyCode.Open);
            _winKeyMap.Add(Keys.F8, KeyCode.Properties);

            // Top key block
            _winKeyMap.Add(Keys.F9, KeyCode.Center);
            _winKeyMap.Add(Keys.F10, KeyCode.Bold);
            _winKeyMap.Add(Keys.F11, KeyCode.Italics);
            _winKeyMap.Add(Keys.F12, KeyCode.Underline);
            _winKeyMap.Add(Keys.PrintScreen, KeyCode.Superscript);
            _winKeyMap.Add(Keys.Scroll, KeyCode.Subscript);
            _winKeyMap.Add(Keys.Pause, KeyCode.LargerSmaller);
            _winKeyMap.Add(Keys.NumLock, KeyCode.Defaults);

            // Right key block
            _winKeyMap.Add(Keys.Home, KeyCode.SkipNext);
            _winKeyMap.Add(Keys.PageUp, KeyCode.Undo);
            _winKeyMap.Add(Keys.End, KeyCode.DefnExpand);
            _winKeyMap.Add(Keys.PageDown, KeyCode.Stop);
            _winKeyMap.Add(Keys.Up, KeyCode.Help);
            _winKeyMap.Add(Keys.Left, KeyCode.Margins);
            _winKeyMap.Add(Keys.OemPipe, KeyCode.Font);       // Mapped to backslash as interlisp uses Font as \ key.
            _winKeyMap.Add(Keys.Down, KeyCode.Keyboard);
        }

        private void InitializeKeymapSDL()
        {
            _sdlKeyMap = new Dictionary<SDL.SDL_Keycode, KeyCode>();

            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_a, KeyCode.A);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_b, KeyCode.B);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_c, KeyCode.C);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_d, KeyCode.D);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_e, KeyCode.E);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_f, KeyCode.F);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_g, KeyCode.G);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_h, KeyCode.H);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_i, KeyCode.I);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_j, KeyCode.J);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_k, KeyCode.K);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_l, KeyCode.L);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_m, KeyCode.M);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_n, KeyCode.N);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_o, KeyCode.O);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_p, KeyCode.P);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_q, KeyCode.Q);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_r, KeyCode.R);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_s, KeyCode.S);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_t, KeyCode.T);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_u, KeyCode.U);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_v, KeyCode.V);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_w, KeyCode.W);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_x, KeyCode.X);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_y, KeyCode.Y);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_z, KeyCode.Z);

            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_0, KeyCode.N0);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_1, KeyCode.N1);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_2, KeyCode.N2);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_3, KeyCode.N3);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_4, KeyCode.N4);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_5, KeyCode.N5);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_6, KeyCode.N6);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_7, KeyCode.N7);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_8, KeyCode.N8);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_9, KeyCode.N9);

            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_EQUALS, KeyCode.Equals);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_MINUS, KeyCode.Minus);

            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_RETURN, KeyCode.Return);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_SPACE, KeyCode.Space);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_BACKSPACE, KeyCode.Backspace);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_LEFTBRACKET, KeyCode.LBracket);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_RIGHTBRACKET, KeyCode.RBracket);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_PERIOD, KeyCode.Period);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_COMMA, KeyCode.Comma);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_SLASH, KeyCode.FSlash);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_SEMICOLON, KeyCode.Colon);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_QUOTE, KeyCode.Quote);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_TAB, KeyCode.Tab);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_RIGHT, KeyCode.FArrow);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_BACKSLASH, KeyCode.Font);   // Mapped to backslash as interlisp uses Font as \ key.
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_CAPSLOCK, KeyCode.Lock);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_BACKQUOTE, KeyCode.BackQuote);

            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_LSHIFT, KeyCode.LeftShift);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_RSHIFT, KeyCode.RightShift);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_LCTRL, KeyCode.Open);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_RCTRL, KeyCode.Properties);

            // Left key block (Open/Properties are also mapped to the Ctrl keys
            // as they are used as Meta/Ctrl in interlisp)
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F1, KeyCode.Again);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F2, KeyCode.Delete);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F3, KeyCode.Find);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F4, KeyCode.Copy);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F5, KeyCode.Same);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F6, KeyCode.Move);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F7, KeyCode.Open);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F8, KeyCode.Properties);

            // Top key block
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F9, KeyCode.Center);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F10, KeyCode.Bold);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F11, KeyCode.Italics);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_F12, KeyCode.Underline);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_PRINTSCREEN, KeyCode.Superscript);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_SCROLLLOCK, KeyCode.Subscript);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_PAUSE, KeyCode.LargerSmaller);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR, KeyCode.Defaults);

            // Right key block
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_HOME, KeyCode.SkipNext);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_PAGEUP, KeyCode.Undo);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_END, KeyCode.DefnExpand);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_PAGEDOWN, KeyCode.Stop);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_UP, KeyCode.Help);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_LEFT, KeyCode.Margins);
            _sdlKeyMap.Add(SDL.SDL_Keycode.SDLK_DOWN, KeyCode.Keyboard);
        }

        /// <summary>
        /// Maps the SDL mouse button to the Star's mouse button.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        private static StarMouseButton GetMouseButtonSDL(byte button)
        {
            StarMouseButton starButton = StarMouseButton.None;

            switch (button)
            {
                case 1:
                    starButton = StarMouseButton.Left;
                    break;

                case 2:
                    starButton = StarMouseButton.Middle;
                    break;

                case 3:
                    starButton = StarMouseButton.Right;
                    break;
            }

            return starButton;
        }

        /// <summary>
        /// Maps the Winforms mouse button to the Star's mouse button.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        private static StarMouseButton GetMouseButtonWinforms(MouseButtons button)
        {
            StarMouseButton starButton = StarMouseButton.None;

            switch (button)
            {
                case MouseButtons.Left:
                    starButton = StarMouseButton.Left;
                    break;

                case MouseButtons.Middle:
                    starButton = StarMouseButton.Middle;
                    break;

                case MouseButtons.Right:
                    starButton = StarMouseButton.Right;
                    break;
            }

            return starButton;
        }

        private void InitializeSDL()
        {
            int retVal;

            // Get SDL humming
            if ((retVal = SDL.SDL_Init(SDL.SDL_INIT_VIDEO)) < 0)
            {
                throw new InvalidOperationException(String.Format("SDL_Init failed.  Error {0:x}", retVal));
            }

            // 
            if (SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "0") == SDL.SDL_bool.SDL_FALSE)
            {
                throw new InvalidOperationException("SDL_SetHint failed to set scale quality.");
            }

            _sdlWindow = SDL.SDL_CreateWindowFrom(DisplayBox.Handle);

            if (_sdlWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException("SDL_CreateWindow failed.");
            }

            if (Configuration.Platform == PlatformType.Unix)
            {
                // On UNIX platforms it appears that asking for hardware acceleration causes SDL_CreateRenderer
                // to hang indefinitely.  For the time being, we'll default to software rendering.
                _sdlRenderer = SDL.SDL_CreateRenderer(_sdlWindow, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);

                if (_sdlRenderer == IntPtr.Zero)
                {
                    // No luck.
                    throw new InvalidOperationException("SDL_CreateRenderer failed.");
                }
            }
            else
            {
                _sdlRenderer = SDL.SDL_CreateRenderer(_sdlWindow, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
                if (_sdlRenderer == IntPtr.Zero)
                {
                    // Fall back to software
                    _sdlRenderer = SDL.SDL_CreateRenderer(_sdlWindow, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);

                    if (_sdlRenderer == IntPtr.Zero)
                    {
                        // Still no luck.
                        throw new InvalidOperationException("SDL_CreateRenderer failed.");
                    }
                }
            }

            _displayTexture = SDL.SDL_CreateTexture(
                _sdlRenderer,
                SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                _displayWidth,
                _displayHeight);

            if (_displayTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException("SDL_CreateTexture failed.");
            }

            SDL.SDL_SetTextureBlendMode(_displayTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL.SDL_SetRenderDrawColor(_sdlRenderer, 0x00, 0x00, 0x00, 0xff);

            // Register a User event for rendering.
            _renderEventType = SDL.SDL_RegisterEvents(1);
            _renderEvent = new SDL.SDL_Event();
            _renderEvent.type = (SDL.SDL_EventType)_renderEventType;
        }

        private DSystem _system;

        //
        // Display data
        //
        //
        // Buffer for rendering pixels.  SDL doesn't support 1bpp pixel formats, so to keep things simple we use
        // an array of ints and a 32bpp format.  What's a few extra bits between friends.
        //
        private int[] _32bppDisplayBuffer = new int[(_displayWidth * _displayHeight)];
        private uint _litPixel;
        private uint _offPixel;
        private delegate void DisplayDelegate();
        private delegate void SDLMessageHandlerDelegate(SDL.SDL_Event e);

        private const uint _litPixelSlow = 0xffeffeff;   // slightly bluish
        private const uint _offPixelSlow = 0x20000000;   // provides a fakey-phosphor persistence

        private const uint _litPixelNormal = 0xffeffeff;   // slightly bluish
        private const uint _offPixelNormal = 0xff000000;   

        private const int _displayWidth = 1088;
        private const int _displayHeight = 860;
        private double _displayScale;
        private int _frameCount;


        //
        // Keyboard data
        //
        private Dictionary<Keys, KeyCode> _winKeyMap;
        private Dictionary<SDL.SDL_Keycode, KeyCode> _sdlKeyMap;
        private bool _capsLock;

        //
        // Mouse data
        //
        private bool _skipNextMouseMove;
        private bool _mouseCaptured;
        private bool _currentCursorState;

        //
        // SDL
        //
        private IntPtr _sdlWindow = IntPtr.Zero;
        private IntPtr _sdlRenderer = IntPtr.Zero;

        private UInt32 _renderEventType;
        private SDL.SDL_Event _renderEvent;

        private Thread _sdlThread;
        private bool _sdlRunning;

        // Rendering textures
        private IntPtr _displayTexture = IntPtr.Zero;
    }
}
