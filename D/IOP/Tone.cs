using D.Logging;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace D.IOP
{
    /// <summary>
    /// Implements the tone generator used to generate simple beeps.
    /// This is driven by an i8253 programmable interval timer.
    /// </summary>
    public class Tone
    {        
        public Tone()
        {
            Reset();
        }

        public void Reset()
        {
            _lsb = 0;
            _loadLSB = true;
            _frequency = 0.0;
            _enabled = false;
            _sampleOn = false;
            _periodInSamples = 0;
        }

        public void LoadInterval(byte value)
        {
            if (_loadLSB)
            {
                _lsb = value;
            }
            else
            {
                //;Frequency constant (1843.2/f, f in kHz)
                _frequency = ((value << 8) | value) / 1.8432;
                if (Log.Enabled) Log.Write(LogComponent.Tone, "Tone frequency set to {0}", _frequency);
                _periodInSamples = (44100.0 / _frequency) / 2;
            }

            _loadLSB = false;
        }
        
        public void EnableTone()
        {
            if (Log.Enabled) Log.Write(LogComponent.Tone, "Tone enabled.", _frequency);
            _enabled = true;
        }

        public void DisableTone()
        {
            // if (Log.Enabled) Log.Write(LogComponent.Tone, "Tone disabled.", _frequency);
            _enabled = false;
        }

        public void AudioCallback(IntPtr userData, IntPtr stream, int length)
        {
            byte[] samples = new byte[length];

            for (int i = 0; i < length; i++)
            {
                _position++;

                if (_position > _periodInSamples)
                {
                    _position -= _periodInSamples;
                    _sampleOn = !_sampleOn;
                }

                samples[i] = (byte)(_enabled ? (_sampleOn ? 0xff : 0x00) : 0x00);
            }

            // Marshal.Copy(samples, 0, stream, length);
        }

        private bool _loadLSB;
        private byte _lsb;

        private double _frequency;
        private bool _enabled;
        private double _position;
        private double _periodInSamples;
        private bool _sampleOn;
    }
}
