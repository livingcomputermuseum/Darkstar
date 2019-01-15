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
using System.Windows.Forms;

namespace D.Debugger
{
    public partial class CPDebugger : Form
    {
        public CPDebugger(DSystem system)
        {
            InitializeComponent();

            _system = system;

            SourceDisplay.SetSourceRoot(Path.Combine("CP", "Source"));

            _loadMap = new MicrocodeLoadMap();
            _loadMap.Load(Path.Combine("CP", "Source", "load_map.txt"));

            _sourceMaps = new List<SourceMap>();

            Disassembly.AttachCP(system.CP);

            PopulateSourceList();
        }        

        public void DisplayCurrentCode()
        {
            RefreshSourceMaps();

            //
            // Select active files in the source file list
            //
            SourceFiles.ClearSelected();
            
            foreach(SourceMap s in _sourceMaps)
            {
                foreach(string fileName in s.GetSourceFiles())
                {
                    for (int i = 0; i < SourceFiles.Items.Count; i++)
                    {
                        if (fileName == (string)SourceFiles.Items[i])
                        {
                            SourceFiles.SetSelected(i, true);
                        }
                    }
                }
            }

            //
            // Find the source line that matches the current TPC, if any.
            //
            int tpc = _system.CP.TPC[(int)_system.CP.CurrentTask];

            foreach (SourceMap s in _sourceMaps)
            {
                SourceEntry entry = s.GetExactSourceForAddress((ushort)tpc);

                if (entry != null)
                {
                    // WE GOT ONE!!!
                    SourceDisplay.AttachMap(s);
                    SourceDisplay.SelectSourceEntry(entry, false, false /* cp */);

                    // Select the source file in the source list
                    for (int i = 0; i < SourceFiles.Items.Count; i++)
                    {
                        if (entry.SourcePath == (string)SourceFiles.Items[i])
                        {
                            SourceFiles.SetSelected(i, true);
                        }
                    }

                    break;
                }
            }

            //
            // Highlight the line in the disassembly as well.
            //
            Disassembly.SelectAddress(tpc);
        }

        public SourceEntry GetSymbolForAddress(int address)
        {
            RefreshSourceMaps();

            SourceEntry entry = null;

            foreach (SourceMap s in _sourceMaps)
            {
                entry = s.GetSourceForAddress((ushort)address);

                if (entry != null)
                {
                    break;
                }
            }

            return entry;
        }

        public void Save()
        {
            SaveSourceMaps();
        }

        private void PopulateSourceList()
        {
            SourceFiles.Items.Clear();

            // TODO: factor out path generation
            IEnumerable<string> files = Directory.EnumerateFiles(Path.Combine("CP", "Source"), "*.mc,v", SearchOption.TopDirectoryOnly);

            foreach(string file in files)
            {
                SourceFiles.Items.Add(Path.GetFileName(file));
            }
        }

        private void OnSourceFilesClicked(object sender, EventArgs e)
        {
            
        }

        private void OnSourceDisplayMouseClick(object sender, MouseEventArgs e)
        {
            //
            // On right click when unmapped file is displayed, we will bring up the map dialog.
            //
            if (e.Button == MouseButtons.Right)
            {
                if (SourceDisplay.ReadOnly)
                {
                    LoadMapDialog mapDialog = new LoadMapDialog(SourceDisplay.CurrentSourceFile, _loadMap, _sourceMaps, _system.CP.MicrocodeRam);
                    DialogResult res = mapDialog.ShowDialog(this);

                    if (res == DialogResult.OK)
                    {
                        RefreshSourceMaps();
                        SourceDisplay.ReadOnly = false;
                        SourceDisplay.AttachMap(mapDialog.SelectedMap);
                    }
                }
            }
        }

        private void OnSourceFilesDoubleClicked(object sender, EventArgs e)
        {
            string selectedFile = (string)SourceFiles.SelectedItem;

            //
            // If none of the source maps contain this file, we will display the source in read-only mode
            // (no annotations allowed).
            //
            RefreshSourceMaps();

            bool mapped = false;            

            foreach (SourceMap m in _sourceMaps)
            {
                if (m.GetSourceFiles().Contains(selectedFile))
                {
                    SourceDisplay.AttachMap(m);
                    mapped = true;
                    break;
                }
            }            

            SourceDisplay.SelectSourceEntry(new SourceEntry((string)SourceFiles.SelectedItem, new string[] { }, 0, 1), !mapped, false /* cp */);
        }

        private void CPDebugger_FormClosed(object sender, FormClosedEventArgs e)
        {
            _loadMap.Save(Path.Combine("CP", "Source", "load_map.txt")); // TODO: move to constant
            SaveSourceMaps();
        }

        private void RefreshSourceMaps()
        {
            List<LoadMapEntry> mapEntries = _loadMap.FindEntries(_system.CP.MicrocodeRam);

            //
            // See if any entries have changed, if so reload source maps.
            //
            bool changed = _mapEntries == null || mapEntries.Count != _mapEntries.Count;

            if (!changed)
            {
                for (int i = 0; i < mapEntries.Count; i++)
                {
                    //
                    // This assumes that ordering remains constant, which works at the moment.
                    //
                    if (mapEntries[i].Name != _mapEntries[i].Name)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                //
                // Commit any existing source maps back to disk.
                //
                SaveSourceMaps();                

                // Build a new set of source maps.
                _sourceMaps.Clear();                
                _mapEntries = mapEntries;

                foreach (LoadMapEntry e in _mapEntries)
                {
                    SourceMap newMap = new SourceMap(
                        e.Name,
                        Path.Combine("CP", "Source", e.MapName), 
                        Path.Combine("CP", "Source"));

                    _sourceMaps.Add(newMap);
                }
            }
        }

        private void SaveSourceMaps()
        {
            foreach (SourceMap m in _sourceMaps)
            {
                m.Save();
            }
        }

        private MicrocodeLoadMap _loadMap;
        private List<LoadMapEntry> _mapEntries;
        private List<SourceMap> _sourceMaps;

        private DSystem _system;

        private void OnSourceDisplaySelectionChanged(object sender, EventArgs e)
        {
            //
            // Sync the disassembly display with the selected source address, if possible.
            //
            int address = SourceDisplay.SelectedAddress;

            if (address != -1 && Disassembly.SelectedAddress != address)
            {
                Disassembly.SelectAddress(address);
            }
        }

        private void OnDisassemblySelectionChanged(object sender, EventArgs e)
        {
            //
            // Sync the source display with the selected disassembly address, if possible.
            //
            int address = Disassembly.SelectedAddress;

            //
            // TODO: we should search through all source maps, not just the current one.
            //
            if (SourceDisplay.SourceMap != null && address != -1 && SourceDisplay.SelectedAddress != address)
            {
                SourceEntry entry = SourceDisplay.SourceMap.GetSourceForAddress((ushort)address);

                if (entry != null && !string.IsNullOrWhiteSpace(entry.SourcePath))
                {
                    SourceDisplay.SelectSourceEntry(entry, false, false /* cp */);
                }
            }
        }
    }
}
