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
using System;
using System.IO;

namespace D.IO
{
    public enum DriveType
    {
        Invalid = 0,
        SA1004 = 1,     // 10MB
        Q2040 = 2,      // Quantum 40MB
        Q2080 = 3,      // Quantum 80MB
    }

    /// <summary>
    /// Geometry for an SA1000-style drive
    /// </summary>
    public struct Geometry
    {
        public Geometry(int cylinders, int heads)
        {
            Cylinders = cylinders;
            Heads = heads;
        }

        public int Cylinders;
        public int Heads;

        public static Geometry SA1004 = new Geometry(256, 4);
        public static Geometry Q2040 = new Geometry(512, 8);
        public static Geometry Q2080 = new Geometry(1172, 7);
    }

    /// <summary>
    /// Encapsulates the state, disk data and low-level behavior of Shugart SA1000-style drives.
    /// </summary>
    public class SA1000Drive
    {
        public SA1000Drive(DSystem system)
        {
            _type = DriveType.Invalid;
            NewDisk(_type, String.Empty);

            _system = system;            

            // Queue up the event that rotates our virtual disk.  This runs continuously.            
            _system.Scheduler.Schedule(_diskWordDelay, DiskWordCallback);

            Reset();
        }

        public void Reset()
        {
            _cylinder = 0;
            _head = 0;
            _wordIndex = 0;
            _index = false;
            _seekComplete = true;
            _lastStep = false;
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty(_diskImagePath))
            {
                Save(_diskImagePath);
            }
        }

        public void Save(string path)
        {
            //
            // We commit to a temporary file first, then replace the original with the
            // temporary file if successful.  This ensures that if something goes wrong
            // during the Save operation that at the very least the original image file
            // is left unscathed.
            string tempPath = Path.GetTempFileName();

            using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                //
                // Format is:
                // 1st Byte: Drive type (see DriveType enumeration)
                // Bytes 2-N: Tracks of data (5325 words each) for all cylinders on the disk
                // Each word in a track is encapsulated in a 24-bit integer:
                // Metadata (address mark, CRC indicators) in upper 8 bits, data in low 16 bits.
                //
                fs.WriteByte((byte)_type);

                for (int cyl = 0; cyl < _geometry.Cylinders; cyl++)
                {
                    for (int head = 0; head < _geometry.Heads; head++)
                    {
                        for (int word = 0; word < _wordsPerTrack; word++)
                        {
                            fs.Write(BitConverter.GetBytes(_tracks[cyl, head, word]), 0, 3);
                        }
                    }
                }
            }

            // Complete:  Move temporary file to final location.
            File.Copy(tempPath, path, true /* overwrite */);
            _diskImagePath = path;
        }

        public void Load(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte type = (byte)fs.ReadByte();
                    if (type < (int)DriveType.SA1004 || type > (int)DriveType.Q2080)
                    {
                        throw new InvalidOperationException("Unsupported drive type.");
                    }

                    _type = (DriveType)type;

                    NewDisk(_type, path);

                    byte[] buffer = new byte[4];
                    for (int cyl = 0; cyl < _geometry.Cylinders; cyl++)
                    {
                        for (int head = 0; head < _geometry.Heads; head++)
                        {
                            for (int word = 0; word < _wordsPerTrack; word++)
                            {
                                int read = fs.Read(buffer, 0, 3);

                                if (read < 3)
                                {
                                    throw new InvalidOperationException("Short read on disk image load.");
                                }

                                _tracks[cyl, head, word] = BitConverter.ToUInt32(buffer, 0);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                //
                // Hit an exception while loading, ensure that we don't
                // have a partial image loaded.
                //
                _type = DriveType.Invalid;
                NewDisk(_type, String.Empty);

                throw e;
            }
        }

        public void NewDisk(DriveType type, string path)
        {
            switch (type)
            {
                case DriveType.Invalid:
                    //
                    // For Invalid (i.e. unloaded or unspecified disks)
                    // we assume an SA1004 geometry, but will always
                    // return Not Ready.
                    //
                case DriveType.SA1004:
                    _geometry = Geometry.SA1004;
                    break;

                case DriveType.Q2040:
                    _geometry = Geometry.Q2040;
                    break;

                case DriveType.Q2080:
                    _geometry = Geometry.Q2080;
                    break;                
            }

            _tracks = new uint[_geometry.Cylinders, _geometry.Heads, _wordsPerTrack];
            _type = type;
            _diskImagePath = path;
        }

        public string ImagePath
        {
            get { return _diskImagePath; }
        }

        public DriveType Type
        {
            get { return _type; }
        }

        public Geometry Geometry
        {
            get { return _geometry; }
        }

        public int WordsPerTrack
        {
            get { return _wordsPerTrack; }
        }

        public int Cylinder
        {
            get { return _cylinder; }
        }

        public int Head
        {
            get { return _head; }
        }

        public bool Track0
        {
            get { return _cylinder == 0; }
        }

        public bool Index
        {
            get { return _index; }
        }

        public bool IsReady
        {
            //
            // We're always spun up and ready as 
            // long as a valid disk is loaded.
            //
            get { return _type != DriveType.Invalid; }
        }

        public bool SeekComplete
        {
            get { return _seekComplete; }
        }

        public int WordIndex
        {
            get { return _wordIndex; }
        }

        public void SetHead(int head)
        {
            _head = head % _geometry.Heads;
        }

        public void Step(bool directionIn, bool stepSignal)
        {            
            if (stepSignal && !_lastStep)
            {
                if (_stepCount  == 0)
                {
                    _directionIn = directionIn;
                }

                // We queue up the step operation
                _timeSinceLastStep = 0;
                _stepCount++;

                _seekComplete = false;

                if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Buffering step.");
            }

            _lastStep = stepSignal;
        }       
        
        public uint ReadData()
        {            
            return _currentWord;
        }

        public void WriteData(ushort data)
        {
            _tracks[_cylinder, _head, _wordIndex] = data;

            if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Wrote 0x{0:x4} to c/h/w {1}/{2}/{3}", data, _cylinder, _head, _wordIndex);
        }

        public void WriteAddressMark(ushort data)
        {
            _tracks[_cylinder, _head, _wordIndex] = (uint)(data | 0x10000);       // Set AM bit

            if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Wrote Address Mark 0x{0:x4} to c/h/w {1}/{2}/{3}", data, _cylinder, _head, _wordIndex);
        }

        public void WriteCRC(ushort data)
        {           
            _tracks[_cylinder, _head, _wordIndex] = (uint)(data | 0x20000);       // Set CRC bit

            if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Wrote CRC 0x{0:x4} to c/h/w {1}/{2}/{3}", data, _cylinder, _head, _wordIndex);            
        }

        public uint DebugRead(int cylinder, int head, int word)
        {
            return _tracks[cylinder, head, word];
        }

        /// <summary>
        /// This gets called back every 4.32uS at which point we move a new word under the head of the disk,
        /// wake up the disk task as appropriate, and deal with buffered seeks if any are in progress.
        /// </summary>
        /// <param name="skewNsec"></param>
        /// <param name="context"></param>
        private void DiskWordCallback(ulong skewNsec, object context)
        {
            //
            // Rotate the disk one word.  If a wakeup is requested then take care of that.           
            //
            SpinDisk();

            //
            // Let the controller know a new word is ready.
            //
            _system.ShugartController.SignalDiskWordReady();

            //
            // Deal with buffered seeks: if more than 350uS has elapsed since the last step
            // pulse from the microcode, we will do the seek now.
            // Technically this logic belongs in the drive itself but since we already have this
            // convenient event running here we do it now.
            //
            _timeSinceLastStep += 370;

            if (_timeSinceLastStep > 35000 && _stepCount > 0)
            {
                Seek(_stepCount, _directionIn);
                _stepCount = 0;
            }

            // Queue this event up again.
            _system.Scheduler.Schedule(_diskWordDelay - skewNsec, DiskWordCallback);
        }

        /// <summary>
        /// Performs a "buffered seek" by the requested amount
        /// in the specified direction.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="directionIn"></param>
        private void Seek(int count, bool directionIn)
        {           
            _destinationCylinder = _cylinder + count * (directionIn ? 1 : -1);

            // Clip into range
            _destinationCylinder = Math.Max(0, _destinationCylinder);
            _destinationCylinder = Math.Min(_geometry.Cylinders - 1, _destinationCylinder);

            //
            // Schedule a seek for about 50ms in the future.
            // This is approximate, but we don't need exact timing here.
            //
            _system.Scheduler.Schedule(_seekDuration, SeekCompleteCallback);
        }

        private void SeekCompleteCallback(ulong skewNsec, object context)
        {
            //
            // Move to the specified destination.
            //
            _cylinder = _destinationCylinder;
            _seekComplete = true;

            _system.ShugartController.SignalSeekComplete();

            if (Log.Enabled) Log.Write(LogComponent.ShugartControl, "Seek to {0} complete.", _cylinder);
        }

        /// <summary>
        /// Simulates the rotation of the disc under the drive's heads; each call
        /// puts a new word of data under the "head" (which is returned by ReadData).  At the end 
        /// of the track the Index signal is raised.
        /// </summary>
        /// <param name="skewNsec"></param>
        /// <param name="context"></param>
        private void SpinDisk()
        {
            _currentWord = _tracks[_cylinder, _head, _wordIndex];

            _wordIndex++;

            if (_wordIndex == _wordsPerTrack)
            {
                _wordIndex = 0;
                _index = true;
            }
            else
            {
                _index = false;
            }
        }


        //
        // Per the SA1000 spec, each track has a capacity of 10.4kbytes -- roughly 5325 words.
        //
        // We store disk data words in 32-bit words; the low 16 bits are the data, the upper 16 bits being non-zero
        // indicates that the word is an Address Mark or a CRC word.
        //
        // The 10mb SA1004 has 256 tracks and 4 heads.
        private Geometry _geometry;
        private DriveType _type;
        private const int _wordsPerTrack = 5325;
        private uint[,,] _tracks;

        private int _wordIndex;
        private uint _currentWord;

        //
        // Disk addressing
        //
        private int _cylinder;
        private int _head;

        //
        // Status bits
        private bool _index;

        //
        // Seek timing and data
        //
        private ulong _seekDuration = (ulong)(25.0 * Conversion.MsecToNsec);        
        private int _destinationCylinder;        
        private bool _seekComplete;

        //
        // Seek status
        //        
        private bool _lastStep;
        private int _stepCount;
        private int _timeSinceLastStep;
        private bool _directionIn;

        //
        // Disk word timing.        
        //
        // Time for a single word to move under the heads: ~3.6uS.
        // (Disk spins at 3125rpm, meaning 0.0192 seconds/revolution.  There are
        // 5325 words per track -- 0.0192 / 5325 = 3.60e-6 seconds, or 3.6uS.
        // To make this line up nicely with the Star microcode clock (just to make things
        // more deterministic) we make the delay 27 microinstruction cycles long -- 
        // 3699nS.
        private readonly ulong _diskWordDelay = 3699;

        //
        // Image file data
        //
        private string _diskImagePath;

        //
        // System
        //
        private DSystem _system;        
    }

}
