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
using System.Security.Cryptography;
using System.Text;

namespace D.Debugger
{

    public struct LoadMapEntry
    {
        public LoadMapEntry(string name, string mapName, int start, int end, byte[] hash)
        {
            Name = name;
            MapName = mapName;
            Start = start;
            End = end;
            Hash = hash;
        }

        /// <summary>
        /// A friendly name for this load (i.e. "Phase 0 Microcode")
        /// </summary>
        public string Name;

        /// <summary>
        /// The file name for the source map for this load
        /// </summary>
        public string MapName;

        /// <summary>
        /// Beginning of address range for microcode load
        /// </summary>
        public int Start;

        /// <summary>
        /// End of range (inclusive)
        /// </summary>
        public int End;

        /// <summary>
        /// MD5 hash of microcode memory for range
        /// </summary>
        public byte[] Hash;
    }

    /// <summary>
    /// MicrocodeLoadMap keeps track of a set of LoadMapEntries, each of which
    /// specifies a map from a memory range + checksum to a source code mapping (i.e. symbol table).
    /// This source code map is identical to the map used for the 8085 source map.
    /// 
    /// This should in theory allow for more-or-less automagical mapping of whatever's in microcode
    /// RAM to the appropriate source files (assuming I've done the gruntwork of actually doing the
    /// mapping beforehand.)
    ///
    /// </summary>
    public class MicrocodeLoadMap
    {
        public MicrocodeLoadMap()
        {
            _mapEntries = new List<LoadMapEntry>();
        }

        public LoadMapEntry AddEntry(string name, int start, int end, ulong[] microcodeRAM)
        {
            if (end <= start || start > microcodeRAM.Length || end > microcodeRAM.Length)
            {
                throw new InvalidOperationException("Invalid start/end parameters.");
            }

            //
            // Ensure no duplicate entries (by name, anyway...)
            //
            foreach(LoadMapEntry e in _mapEntries)
            {
                if (e.Name.ToLowerInvariant() == name.ToLowerInvariant())
                {
                    throw new InvalidOperationException("Duplicate map entry name.");
                }
            }

            // Generate a map name
            string mapName = name + "_map.txt";

            // If the map file doesn't exist, create it now.
            

            // calculate the MD5 hash
            byte[] hash = ComputeHash(start, end, microcodeRAM);

            LoadMapEntry newEntry = new LoadMapEntry(name, mapName, start, end, hash);

            _mapEntries.Add(newEntry);

            return newEntry;
        }

        public List<LoadMapEntry> FindEntries(ulong[] microcodeRAM)
        {
            //
            // Given the provided microcode RAM, Walk the entries we know about and
            // see which ones match, if any.
            //
            List<LoadMapEntry> foundEntries = new List<LoadMapEntry>();

            foreach(LoadMapEntry e in _mapEntries)
            {
                //
                // Hash the memory range specified by this entry and see if it matches.
                //
                byte[] hash = ComputeHash(e.Start, e.End, microcodeRAM);
                bool match = true;

                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != e.Hash[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    foundEntries.Add(e);
                }
            }

            return foundEntries;
        }

        public void Save(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                //
                // Each entry looks like:
                //
                // <name>,<mapname>,<start>,<end>,<hash>
                // where:
                //  <name> and <mapname> are strings,
                //  <start> and <end> are hexadecimal values
                //  <hash> is written as a series of ascii hex digits
                //
                // empty lines or lines beginning with "#" are ignored.
                //
                // And that's it!
                //
                foreach (LoadMapEntry e in _mapEntries)
                {
                    StringBuilder hashText = new StringBuilder();
                    for (int i = 0; i < e.Hash.Length; i++)
                    {
                        hashText.AppendFormat("{0:x2}", e.Hash[i]);
                    }

                    sw.WriteLine("{0},{1},{2:x3},{3:x3},{4}", e.Name, e.MapName, e.Start, e.End, hashText.ToString());
                }
            }
        }

        public void Load(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                _mapEntries.Clear();

                //
                // See "Save" for the format we're dealing with here.
                //
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] tokens = line.Split(',');

                    string hashString = tokens[4].Trim();
                    byte[] hash = new byte[hashString.Length / 2];

                    for (int i = 0; i < hashString.Length; i += 2)
                    {
                        hash[i / 2] = Convert.ToByte(hashString.Substring(i, 2), 16);
                    }

                    LoadMapEntry e = new LoadMapEntry(
                        tokens[0],  // name
                        tokens[1],  // mapname
                        Convert.ToInt32(tokens[2].Trim(), 16),      // start
                        Convert.ToInt32(tokens[3].Trim(), 16),      // end
                        hash);

                    _mapEntries.Add(e);
                }
            }
        }

        private byte[] ComputeHash(int start, int end, ulong[] microcodeRAM)
        {
            //
            // Create a byte[] of the microcode data because why not.
            //
            byte[] microcodeBytes = new byte[(end - start) * 8];
            int microcodeIndex = 0;

            for (int i = start; i < end; i++)
            {
                byte[] wordBytes = BitConverter.GetBytes(microcodeRAM[i]);
                wordBytes.CopyTo(microcodeBytes, microcodeIndex);
                microcodeIndex += 8;
            }

            MD5 md5 = MD5.Create();
            return md5.ComputeHash(microcodeBytes);
        }

        private List<LoadMapEntry> _mapEntries;
    }
}
