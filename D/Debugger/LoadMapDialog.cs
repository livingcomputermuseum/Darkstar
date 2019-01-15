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
    public partial class LoadMapDialog : Form
    {
        /// <summary>
        /// Provide basic UI for adding new entries.
        /// TODO: It's kind of ugly that this is directly modifying input parameters; likely there should be a
        /// singleton LoadMap that maintains all of this that can be used to modify things.        
        /// </summary>
        /// <param name="newFilePath"></param>
        /// <param name="loadMap"></param>
        /// <param name="sourceMaps"></param>
        /// <param name="microcodeRAM"></param>
        public LoadMapDialog(string newFilePath, MicrocodeLoadMap loadMap, List<SourceMap> sourceMaps, ulong[] microcodeRAM)
        {
            InitializeComponent();

            _newFilePath = newFilePath;
            _loadMap = loadMap;
            _sourceMaps = sourceMaps;
            _microcodeRAM = microcodeRAM;

            _selectedMap = null;

            //
            // Populate the existing map list
            //
            foreach (SourceMap s in sourceMaps)
            {
                CurrentLoadsList.Items.Add(s.MapName);
            }

            CurrentLoadsList.ClearSelected();

            DialogResult = DialogResult.Cancel;
        }

        public SourceMap SelectedMap
        {
            get { return _selectedMap; }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            AddButton.Enabled = CurrentLoadsList.SelectedItem != null;
        }        

        private void CancelAddButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void CancelCreateButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            //
            // Add the file to the selected map.
            //
            _selectedMap = _sourceMaps[CurrentLoadsList.SelectedIndex];

            _selectedMap.AddSourceFile(_newFilePath);
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CreateLoadButton_Click(object sender, EventArgs e)
        {
            try
            {
                LoadMapEntry newEntry = _loadMap.AddEntry(
                    LoadNameText.Text,
                    Convert.ToInt32(LoadStartBox.Text.Trim(), 16),
                    Convert.ToInt32(LoadEndBox.Text.Trim(), 16),
                    _microcodeRAM
                    );

                _selectedMap = new SourceMap(
                        newEntry.Name,
                        Path.Combine("CP", "Source", newEntry.MapName),
                        Path.Combine("CP", "Source"));     // TODO: define this path somewheres.

                _sourceMaps.Add(_selectedMap); 

                DialogResult = DialogResult.OK;
                this.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error: {0}", ex.Message);
            }
        }

        private SourceMap _selectedMap;

        private string _newFilePath;
        private MicrocodeLoadMap _loadMap;
        private List<SourceMap> _sourceMaps;
        private ulong[] _microcodeRAM;        
    }
}
