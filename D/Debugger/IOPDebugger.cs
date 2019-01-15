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
    public partial class IOPDebugger : Form
    {
        public IOPDebugger(DSystem system)
        {
            InitializeComponent();

            _system = system;

            _sourceMap = new SourceMap("8085 ROM", "IOP\\Source\\SourceMap.txt", "IOP\\Source");

            SourceDisplay.SetSourceRoot(Path.Combine("IOP", "Source"));
            SourceDisplay.AttachMap(_sourceMap);

            PopulateSourceList();

            DisplayCurrentCode();
        }

        public SourceMap SourceMap
        {
            get { return _sourceMap; }
        }

        public void DisplayCurrentCode()
        {
            SourceEntry entry = _sourceMap.GetSourceForAddress(_system.IOP.CPU.PC);

            if (entry != null && !string.IsNullOrWhiteSpace(entry.SourcePath))
            {
                SourceDisplay.SelectSourceEntry(entry, false, true /* iop */);


                // Find the source entry in the file list and select it.
                for (int i = 0; i < SourceFiles.Items.Count; i++)
                {
                    if ((string)SourceFiles.Items[i] == entry.SourcePath)
                    {
                        SourceFiles.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                // Should show disassembly instead
            }
        }

        public void Save()
        {
            _sourceMap.Save();
        }

        private void PopulateSourceList()
        {
            SourceFiles.Items.Clear();

            // TODO: factor out path generation
            IEnumerable<string> files = Directory.EnumerateFiles(Path.Combine("IOP", "Source"), "*.asm,v", SearchOption.TopDirectoryOnly);

            foreach(string file in files)
            {
                SourceFiles.Items.Add(Path.GetFileName(file));
            }
        }        

        private void SourceFiles_DoubleClick(object sender, EventArgs e)
        {
            SourceDisplay.SelectSourceEntry(new SourceEntry((string)SourceFiles.SelectedItem, new string[] { }, 0, 1), false, true /* iop */);
        }

        private void IOPDebugger_FormClosed(object sender, FormClosedEventArgs e)
        {
            _sourceMap.Save();
        }       

        private SourceMap _sourceMap;

        private DSystem _system;

        
    }
}
