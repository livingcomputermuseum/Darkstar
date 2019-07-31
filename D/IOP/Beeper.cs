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
    public class Beeper
    {        
        public Beeper()
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

            _sampleBuffer = new byte[0x10000];
        }

        public void LoadPeriod(byte value)
        {
            if (_loadLSB)
            {
                _lsb = value;
            }
            else
            {
                // "The period (in usec*1.8432) will be in the range ~29..65535."
                double period = ((value << 8) | _lsb) / 1.8432;

                // The above is in usec; convert to seconds:
                period = period * Conversion.UsecToSec;

                // Invert to get the frequency in Hz:
                _frequency = 1.0 / period;

                if (Log.Enabled) Log.Write(LogComponent.Beeper, "Tone frequency set to {0}", _frequency);

                // And find out the length in samples at 44.1Khz.
                _periodInSamples = 44100.0 / _frequency;
            }

            _loadLSB = !_loadLSB;
        }
        
        public void EnableTone()
        {
            if (Log.Enabled) Log.Write(LogComponent.Beeper, "Tone enabled.", _frequency);
            _enabled = true;
        }

        public void DisableTone()
        {
            if (Log.Enabled && _enabled) Log.Write(LogComponent.Beeper, "Tone disabled.");
            _enabled = false;
        }

        public void AudioCallback(IntPtr userData, IntPtr stream, int length)
        {
            if (_enabled)
            {
                for (int i = 0; i < length; i++)
                {
                    _position++;

                    if (_position > _periodInSamples)
                    {
                        _position -= _periodInSamples;
                        _sampleOn = !_sampleOn;
                    }

                    _sampleBuffer[i] = (byte)(_enabled ? (_sampleOn ? 0x3f : 0x00) : 0x00);
                }
            }
            else
            {
                Array.Clear(_sampleBuffer, 0, length);
            }

            Marshal.Copy(_sampleBuffer, 0, stream, length);
        }

        private byte[] _sampleBuffer;

        private bool _loadLSB;
        private byte _lsb;

        private double _frequency;
        private bool _enabled;
        private double _position;
        private double _periodInSamples;
        private bool _sampleOn;
    }
}
