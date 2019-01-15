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
using System.Linq;
using System.Text;

namespace D.Debugger
{
    public class SourceEntry
    {
        public SourceEntry(string sourcePath, string[] symbolNames, ushort address, int lineNumber)
        {
            SourcePath = sourcePath;
            SymbolNames = symbolNames;
            Address = address;
            LineNumber = lineNumber;
        }

        public override string ToString()
        {
            return String.Format("{0}, line {1} address 0x{2:x4}", SourcePath, LineNumber, Address);
        }

        public string SourcePath;
        public string[] SymbolNames;
        public ushort Address;
        public int LineNumber;

        public static readonly SourceEntry Empty = new SourceEntry(String.Empty, new string[] { "*none*" }, 0, 0);
    }

    public class SourceMap
    {
        public SourceMap(string mapName, string mapFile, string sourceRoot)
        {            
            _sourceFileToSourceEntryMap = new Dictionary<string, List<SourceEntry>>();
            _orderedSourceEntries = new List<SourceEntry>();

            ReadMap(mapFile, sourceRoot);

            _mapName = mapName;
            _mapFile = mapFile;
            _sourceRoot = sourceRoot;
        }

        public string MapName
        {
            get { return _mapName; }
        }

        public string SourceRoot
        {
            get { return _sourceRoot; }
        }

        /// <summary>
        /// Returns the list of source files referenced by this map.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSourceFiles()
        {
            return _sourceFileToSourceEntryMap.Keys.ToList();
        }

        public void Save()
        {
            string header = 
              @"# 
                # This file maps assembly source symbols to PROM/microcode addresses and source lines, forming a crude
                # symbol table.
                #
                # Each source file is given a header a la:
                # [FooSource.asm]
                # (with brackets)

                # Each line's syntax is:
                # <symbol name 1>, .. , <symbol name N>: <address or value (hex)>,<line number(decimal) in current source file>
                # where '*none*' is a special symbol name meaning no symbol mapping is present.";

            using (StreamWriter sw = new StreamWriter(_mapFile))
            {
                // Write out a nice header
                sw.Write(header);

                sw.WriteLine();

                // Write out each source file
                foreach(string sourceFile in _sourceFileToSourceEntryMap.Keys)
                {
                    sw.WriteLine("[{0}]", sourceFile);

                    foreach(SourceEntry entry in _sourceFileToSourceEntryMap[sourceFile])
                    {
                        StringBuilder symbolList = new StringBuilder();
                        for (int i = 0; i < entry.SymbolNames.Length; i++)
                        {
                            symbolList.AppendFormat(i < entry.SymbolNames.Length - 1 ? "{0}," : "{0}", entry.SymbolNames[i]);
                        }

                        sw.WriteLine("{0}: 0x{1:x4},{2}", symbolList.ToString(), entry.Address, entry.LineNumber + 1);   // line numbers are 1-indexed
                    }

                    sw.WriteLine();
                }
            }
        }

        public SourceEntry GetSourceForAddress(ushort address)
        {
            SourceEntry result = null;

            //
            // Find the SourceEntry nearest this address, if there is one.
            //
            foreach(SourceEntry entry in _orderedSourceEntries)
            {
                if (entry.Address > address)
                {
                    break;
                }

                result = entry;
            }

            return result;
        }

        public SourceEntry GetExactSourceForAddress(ushort address)
        {
            //
            // Find the SourceEntry for this address, if there is one.
            //
            foreach (SourceEntry entry in _orderedSourceEntries)
            {
                if (entry.Address == address)
                {
                    return entry;
                }
            }

            return null;
        }

        public SourceEntry GetNearestSymbolForAddress(ushort address)
        {
            SourceEntry result = null;

            //
            // Find the SourceEntry nearest this address that has a symbol name defined, if there is one.
            //
            foreach (SourceEntry entry in _orderedSourceEntries)
            {
                if (entry.Address > address)
                {
                    break;
                }

                if (entry.SymbolNames.Length > 0 &&
                    entry.SymbolNames[0] != "*none*")   // TODO: move to constant
                {
                    result = entry;
                }
            }

            return result;
        }

        public bool GetAddressForSource(SourceEntry entry, out ushort address)
        {
            bool found = false;
            address = 0;
            
            //
            // Find the source line entry that matches.
            //
            if (_sourceFileToSourceEntryMap.ContainsKey(entry.SourcePath))
            {
                foreach(SourceEntry line in _sourceFileToSourceEntryMap[entry.SourcePath])
                {
                    if (line.LineNumber == entry.LineNumber)
                    {
                        found = true;
                        address = line.Address;
                        break;
                    }
                }
            }

            return found;
        }

        public void AddSourceEntry(SourceEntry entry)
        {         
            // InsertAddressEntry will ensure that no duplicate addresses are added.
            InsertAddressEntry(entry);
            InsertSourceEntry(entry);
        }

        public void AddSourceFile(string sourcePath)
        {
            if (!_sourceFileToSourceEntryMap.ContainsKey(sourcePath))
            {
                List<SourceEntry> newList = new List<SourceEntry>();                
                _sourceFileToSourceEntryMap.Add(sourcePath, newList);
            }
            else
            {
                throw new InvalidOperationException("Source file already exists in map.");
            }
        }

        public void RemoveSourceEntry(SourceEntry entry)
        {
            // Remove from address table
            for (int i = 0; i < _orderedSourceEntries.Count; i++)
            {
                if (_orderedSourceEntries[i].Address == entry.Address)
                {
                    _orderedSourceEntries.RemoveAt(i);
                    break;
                }
            }

            // Remove from source table if present
            if (_sourceFileToSourceEntryMap.ContainsKey(entry.SourcePath))
            {
                for (int i = 0; i < _sourceFileToSourceEntryMap[entry.SourcePath].Count; i++)
                {
                    if (_sourceFileToSourceEntryMap[entry.SourcePath][i].Address == entry.Address)
                    {
                        _sourceFileToSourceEntryMap[entry.SourcePath].RemoveAt(i);
                        break;
                    }
                }
            }
            
        }

        private void ReadMap(string mapFile, string sourceRoot)
        {
            //
            // If the file does not exist, we will just start with an empty map.
            //
            if (!File.Exists(mapFile))
            {
                return;
            }

            using (StreamReader map = new StreamReader(mapFile))
            {
                string sourceFile = string.Empty;
                ReadState state = ReadState.NextFileHeader;
                int mapLineNumber = 0;

                while (!map.EndOfStream)
                {
                    string line = map.ReadLine().Trim();
                    mapLineNumber++;

                    if (string.IsNullOrWhiteSpace(line) ||
                        line.StartsWith("#"))
                    {
                        // Nothing of note here, continue.
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        // Looks like the start of a file header.
                        state = ReadState.NextFileHeader;
                    }

                    switch(state)
                    {
                        case ReadState.NextFileHeader:
                            // We expect the line to be in the form "[<source path>]"
                            // If this is not the case, then the map file is incorrectly formed.
                            if (line.StartsWith("["))
                            {
                                int closingBracket = line.LastIndexOf(']');

                                if (closingBracket < 0)
                                {
                                    throw new InvalidOperationException(
                                        String.Format("Badly formed source file entry on line {0}", mapLineNumber));
                                }

                                sourceFile = line.Substring(1, closingBracket - 1).Trim();
                                state = ReadState.NextSymbolEntry;
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    String.Format("Expected file header on line {0}", mapLineNumber));
                            }
                            break;

                        case ReadState.NextSymbolEntry:
                            //
                            // This is expected to be a symbol map entry, which looks like
                            // <symbol name 1>, .. , <symbol name N> : <address or value (hex)>,<line number(decimal) in current source file>
                            //
                            string[] symbolAddressTokens = line.Split(':');

                            if (symbolAddressTokens.Length != 2)
                            {
                                // Should be two tokens here, one on each side of the ':'
                                throw new InvalidOperationException(
                                    String.Format("Badly formed symbol entry on line {0}", mapLineNumber));
                            }

                            // Grab the symbol names.  There must be at least one present since the above split succeeded.
                            string[] symbolNames = symbolAddressTokens[0].Trim().Split(',');

                            // Grab the source information.  There must be exactly two entries.
                            string[] sourceData = symbolAddressTokens[1].Trim().Split(',');

                            if (sourceData.Length != 2)
                            {
                                throw new InvalidOperationException(
                                    String.Format("Badly formed symbol entry on line {0} -- source information is invalid.", mapLineNumber));
                            }

                            // Convert source info into integers and build a new SourceEntry.
                            int lineNumber = 0;
                            ushort address = 0;
                            try
                            {
                                address = (ushort)Convert.ToInt32(sourceData[0].Trim(), 16);
                                lineNumber = int.Parse(sourceData[1].Trim()) - 1;       // line numbers are 1-indexed
                            }
                            catch(Exception)
                            {
                                throw new InvalidOperationException(
                                    String.Format("Badly formed symbol entry on line {0} -- source information is invalid.", mapLineNumber));
                            }

                            SourceEntry newEntry = new SourceEntry(sourceFile, symbolNames, address, lineNumber);

                            AddSourceEntry(newEntry);
                            break;
                    }
                }
            }
        }

        private void InsertAddressEntry(SourceEntry entry)
        {
            if (_orderedSourceEntries.Count == 0)
            {
                _orderedSourceEntries.Add(entry);
                return;
            }

            // Find the first entry that has an address greater than the new entry's.
            //
            for(int i=0;i<_orderedSourceEntries.Count;i++)
            {
                // Sanity check -- if these entries have equal addresses then we need to stop here.
                if (_orderedSourceEntries[i].Address == entry.Address)
                {
                    throw new InvalidOperationException(
                        String.Format("Duplicate address {0:x4} in source map.", entry.Address));
                }

                if (_orderedSourceEntries[i].Address > entry.Address)
                {
                    _orderedSourceEntries.Insert(i, entry);
                    return;
                }
            }

            //
            // If we get here, then this address is greater than any already in the list, so we add it at the end.
            //
            _orderedSourceEntries.Add(entry);
        }

        private void InsertSourceEntry(SourceEntry entry)
        {
            if (!_sourceFileToSourceEntryMap.ContainsKey(entry.SourcePath))
            {
                List<SourceEntry> newList = new List<SourceEntry>();
                newList.Add(entry);
                _sourceFileToSourceEntryMap.Add(entry.SourcePath, newList);
            }
            else
            {
                _sourceFileToSourceEntryMap[entry.SourcePath].Add(entry);
            }
        }

        //
        // Maps for quick lookups
        //
        private Dictionary<string, List<SourceEntry>> _sourceFileToSourceEntryMap;

        //
        // Ordered list for quick search by address
        //
        private List<SourceEntry> _orderedSourceEntries;

        private string _mapName;
        private string _mapFile;
        private string _sourceRoot;

        enum ReadState
        {
            NextFileHeader,
            NextSymbolEntry,
        }
    }
}
