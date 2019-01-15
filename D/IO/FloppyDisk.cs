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
using System.IO;
using System.Text;

namespace D.IO
{
    /// <summary>
    /// Presents data for a floppy disk, organized by cylinder, head, and sector,
    /// and provides constructors for loading from IMD file.
    /// </summary>
    public class FloppyDisk
    {
        public FloppyDisk(string imagePath)
        {
            _imagePath = imagePath;
            _tracks = new Track[2, 77];
            _isSingleSided = true;

            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                LoadIMD(fs);
            }
        }

        public string Description
        {
            get { return _imdHeader; }
        }

        public bool IsSingleSided
        {
            get { return _isSingleSided; }
        }

        public string ImagePath
        {
            get { return _imagePath; }
        }

        /// <summary>
        /// Returns sector data for the given address.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <param name="head"></param>
        /// <param name="sector"></param>
        /// <returns></returns>
        public Sector GetSector(int cylinder, int head, int sector)
        {
            return _tracks[head, cylinder].ReadSector(sector);
        }

        public Track GetTrack(int cylinder, int head)
        {
            return _tracks[head, cylinder];
        }

        private void LoadIMD(Stream s)
        {
            _imdHeader = ReadIMDHeader(s);

            //
            // Read each track in and place it in memory.
            // We assume that there will be no more than 77 cylinders
            // and no more than 2 tracks.  We also do a basic sanity
            // check that no track appears more than once.
            //
            while (true)
            {
                Track t = new Track(s);

                if (t.Cylinder < 0 || t.Cylinder > 76)
                {
                    throw new InvalidOperationException(String.Format("Invalid cylinder value {0}", t.Cylinder));
                }

                if (t.Head < 0 || t.Head > 1)
                {
                    throw new InvalidOperationException(String.Format("Invalid head value {0}", t.Head));
                }

                if (_tracks[t.Head, t.Cylinder] != null)
                {
                    throw new InvalidOperationException(String.Format("Duplicate head/track", t.Head, t.Cylinder));
                }
                
                if (t.Head != 0)
                {
                    // Got a track on side 1, this must be a double-sided disk.
                    _isSingleSided = false;
                }

                _tracks[t.Head, t.Cylinder] = t;

                if (s.Position == s.Length)
                {
                    // End of file.
                    break;
                }
            }
        }

        private string ReadIMDHeader(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                byte b = (byte)s.ReadByte();

                if (b == 0x1a)
                {
                    break;
                }
                else
                {
                    sb.Append((char)b);
                }
            }

            return sb.ToString();
        }

        private string _imdHeader;
        private bool _isSingleSided;
        private string _imagePath;

        private Track[,] _tracks;
    }

    /// <summary>
    /// Represents a single track's worth of sectors
    /// </summary>
    public class Track
    {
        /// <summary>
        /// Create a new, empty track with the specified format, sector size and sector count.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="cylinder"></param>
        /// <param name="head"></param>
        /// <param name="sectorCount"></param>
        /// <param name="sectorSize"></param>
        public Track(Format format, int cylinder, int head, int sectorCount, int sectorSize)
        {
            _format = format;
            _cylinder = cylinder;
            _head = head;
            _sectorCount = sectorCount;
            _sectorSize = sectorSize;
            _sectors = new Sector[_sectorCount];

            for (int i = 0; i < _sectorCount; i++)
            {
                _sectors[i] = new Sector(_sectorSize, _format);
            }
        }

        /// <summary>
        /// Create a new track loaded from the given stream.  The stream is expected to be positioned
        /// at the beginning of an IMD sector definition.
        /// </summary>
        /// <param name="s"></param>
        public Track(Stream s)
        {
            bool bCylMap = false;
            bool bHeadMap = false;

            _format = (Format)s.ReadByte();
            _cylinder = s.ReadByte();
            _head = s.ReadByte();
            _sectorCount = s.ReadByte();
            int sectorSizeIndex = s.ReadByte();

            // Basic sanity check of values
            if (_format > Format.MFM250 ||
                _cylinder > 77 ||
                (_head & 0x3f) > 1 ||
                sectorSizeIndex > _sectorSizes.Length - 1)
            {
                throw new InvalidOperationException("Invalid header data for track.");
            }

            _sectorSize = _sectorSizes[sectorSizeIndex];

            bCylMap = (_head & 0x80) != 0;
            bHeadMap = (_head & 0x40) != 0;

            // Head is just the first bit.
            _head = (byte)(_head & 0x1);

            //
            // Read sector numbering
            //
            _sectorOrdering = new List<int>(_sectorCount);

            for (int i = 0; i < _sectorCount; i++)
            {
                _sectorOrdering.Add(s.ReadByte());
            }

            //
            // At this time, cyl and head maps are not supported.
            // It's not expected any Star disk would use such a format.
            //
            if (bCylMap | bHeadMap)
            {
                throw new NotImplementedException("IMD Cylinder and Head maps not supported.");
            }

            //
            // Read the sector data in.
            //
            _sectors = new Sector[_sectorCount];
            for (int i = 0; i < _sectorCount; i++)
            {
                SectorRecordType type = (SectorRecordType)s.ReadByte();
                byte compressedData;

                switch (type)
                {
                    case SectorRecordType.Unavailable:
                        // Nothing, sectors left null.
                        break;

                    case SectorRecordType.Normal:
                    case SectorRecordType.NormalDeleted:
                    case SectorRecordType.NormalError:
                    case SectorRecordType.DeletedError:
                        _sectors[_sectorOrdering[i] - 1] = new Sector(_sectorSize, _format, s);
                        break;

                    case SectorRecordType.Compressed:
                    case SectorRecordType.CompressedDeleted:
                    case SectorRecordType.CompressedError:
                    case SectorRecordType.CompressedDeletedError:
                        compressedData = (byte)s.ReadByte();

                        // Fill sector with compressed data
                        _sectors[_sectorOrdering[i] - 1] = new Sector(_sectorSize, _format, compressedData);
                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Unexpected IMD sector data type {0}", type));
                }
            }
        }

        public int Cylinder
        {
            get { return _cylinder; }
        }

        public int Head
        {
            get { return _head; }
        }

        public int SectorCount
        {
            get { return _sectorCount; }
        }

        public int SectorSize
        {
            get { return _sectorSize; }
        }

        public Format Format
        {
            get { return _format; }
        }

        public Sector ReadSector(int sector)
        {
            return _sectors[sector];
        }

        //
        // 00      Sector data unavailable - could not be read
        // 01 .... Normal data: (Sector Size) bytes follow
        // 02 xx Compressed: All bytes in sector have same value(xx)
        // 03 .... Normal data with "Deleted-Data address mark"
        // 04 xx Compressed  with "Deleted-Data address mark"
        // 05 .... Normal data read with data error
        // 06 xx Compressed  read with data error
        // 07 .... Deleted data read with data error
        // 08 xx Compressed, Deleted read with data error
        //
        private enum SectorRecordType
        {
            Unavailable = 0,
            Normal = 1,
            Compressed = 2,
            NormalDeleted = 3,
            CompressedDeleted = 4,
            NormalError = 5,
            CompressedError = 6,
            DeletedError = 7,
            CompressedDeletedError = 8,
        }

        private Format _format;
        private int _cylinder;
        private int _head;
        private int _sectorCount;
        private int _sectorSize;

        private List<int> _sectorOrdering;

        private Sector[] _sectors;

        private static int[] _sectorSizes = { 128, 256, 512, 1024, 2048, 4096, 8192 };
    }

    public class Sector
    {
        public Sector(int sectorSize, Format format)
        {
            _data = new byte[sectorSize];
            _format = format;
        }

        public Sector(int sectorSize, Format format, byte compressedValue)
            : this(sectorSize, format)
        {
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = compressedValue;
            }
        }

        public Sector(int sectorSize, Format format, Stream s)
            : this(sectorSize, format)
        {
            int read = s.Read(_data, 0, sectorSize);

            if (read != sectorSize)
            {
                throw new InvalidOperationException("Short read in sector data.");
            }
        }

        public Format Format
        {
            get { return _format; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        private Format _format;

        private byte[] _data;
    }

    // 00 = 500 kbps FM   \   Note:   kbps indicates transfer rate,
    // 01 = 300 kbps FM    >          not the data rate, which is
    // 02 = 250 kbps FM   /           1/2 for FM encoding.
    // 03 = 500 kbps MFM
    // 04 = 300 kbps MFM
    // 05 = 250 kbps MFM
    public enum Format
    {
        FM500 = 0,
        FM300 = 1,
        FM250 = 2,
        MFM500 = 3,
        MFM300 = 4,
        MFM250 = 5,
    }
}
