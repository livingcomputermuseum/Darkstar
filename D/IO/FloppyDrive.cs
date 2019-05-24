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

using D.IO;
using D.Logging;

namespace D
{
    public class FloppyDrive
    {
        public FloppyDrive(DSystem system)
        {
            _system = system;        
            //
            // Start the Index event rolling.  This will run forever.
            //
            _system.Scheduler.Schedule(_indexInterval, IndexCallback);

            Reset();
        }

        public FloppyDisk Disk
        {
            get { return _disk; }
        }

        public int Track
        {
            get { return _track; }
        }

        public bool IsLoaded
        {
            get { return _disk != null; }
        }

        public bool IsWriteProtected
        {
            get { return _disk != null ? _disk.IsWriteProtected : false; }
        }

        public bool IsSingleSided
        {
            get { return _singleSided; }
        }

        public bool Track0
        {
            get { return _track == 0; }
        }

        public bool Index
        {
            get { return _index; }
        }

        public bool DiskChange
        {
            get { return _diskChange; }
        }

        public bool DriveSelect
        {
            get { return _driveSelect; }
            set
            {
                _driveSelect = value;

                //
                // The Disk Change signal is reset when
                // Drive Select goes low.
                //
                if (!_driveSelect)
                {
                    _diskChange = false;
                }
            }
        }

        public void Reset()
        {
            _track = 0;
            _singleSided = false;
            _diskChange = false;
            _index = false;
            _driveSelect = false;
        }

        public void LoadDisk(FloppyDisk disk)
        {
            _disk = disk;
            _singleSided = _disk.IsSingleSided;
            _diskChange = true;

            if (Log.Enabled) Log.Write(LogComponent.IOPFloppy, "Floppy disk image loaded.  Description is:\n{0}", disk.Description);

            // TODO: update WP, SS bits, etc.
        }

        public void UnloadDisk()
        {
            if (_disk != null && _disk.IsModified)
            {
                _disk.Save();
            }

            _disk = null;
            _diskChange = true;
        }

        public void SeekTo(int track)
        {
            // Clip into range.
            _track = Math.Max(0, track);
            _track = Math.Min(76, _track);
        }

        private void IndexCallback(ulong skewNsec, object context)
        {
            //
            // This always runs even when a disk isn't loaded or spinning.
            // It only actually modifies _index if a disk is loaded.
            // We need to generate index pulses anyway (even if they are 
            // inaccurate and unused by us given that we're not emulating 
            // the disk at that low of a level) this does the job.
            // 
            if (DriveSelect && IsLoaded && !_index)
            {
                // Raise the index signal, hold for a short period.
                _index = true;                
                _system.Scheduler.Schedule(_indexDuration, IndexCallback);

                if (Log.Enabled) Log.Write(LogComponent.IOPFloppy, "Disk rotation complete, raising INDEX signal for 10us.");
            }
            else
            {
                // Reset the index signal, wait for a long period (for the disk to go round again).
                _index = false;
                _system.Scheduler.Schedule(_indexInterval, IndexCallback);
            }

        }

        private DSystem _system;

        private bool _singleSided;
        private int _track;
        private bool _diskChange;        
        private bool _driveSelect;
        private FloppyDisk _disk;

        // Index signal and timing
        private bool _index;
        private ulong _indexInterval = 250 * Conversion.MsecToNsec;  // 1/5 second at 300rpm
        private ulong _indexDuration = 10 * Conversion.UsecToNsec; // 10uSec duration for index signal.        
    }
}
