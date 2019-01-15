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
using System.Windows.Forms;

namespace D.Debugger
{
    /// <summary>
    /// Presents a source code view aligned with symbol addresses (where applicable)
    /// and breakpoints.  One source file is loaded into a SourceDisplay at a time.
    /// 
    /// SourceDisplay provides a three-column view:
    /// 
    /// | Breakpoint | Address | Source text                                    |
    /// -------------------------------------------------------------------------
    /// 
    /// Breakpoint and Address are editable (to allow setting breakpoints and to
    /// allow modifying symbol table data)
    /// 
    /// </summary>
    public class SourceDisplay : DataGridView
    {
        public SourceDisplay()
        {
            this.VirtualMode = true;
            this.RowHeadersVisible = false;

            AddCheckboxColumn("B", DataGridViewAutoSizeColumnMode.ColumnHeader);
            AddColumn("Address", false, DataGridViewAutoSizeColumnMode.ColumnHeader);
            AddColumn("Source", true, DataGridViewAutoSizeColumnMode.Fill);
        }
        
        public string CurrentSourceFile
        {
            get { return _currentSourceFile; }
        }

        /// <summary>
        /// Returns the address of the selected line, if any has been assigned.  Returns
        /// -1 if there is no selection or if no address is available for the selected line.
        /// </summary>
        public int SelectedAddress
        {
            get
            {
                int address = -1;

                if (_sourceMap != null && 
                    _currentSourceFile != null && 
                    this.SelectedCells.Count > 0)
                {
                    int rowIndex = this.SelectedCells[0].RowIndex;
                    // TODO: factor this logic out w/cell input logic.
                    string cellValue = (string)this.Rows[rowIndex].Cells[1].Value;

                    try
                    {
                        // strip leading $ if any.
                        if (cellValue.StartsWith("$"))
                        {
                            cellValue = cellValue.Substring(1);
                        }

                        address = Convert.ToUInt16(cellValue, 16);
                    }
                    catch
                    {
                        address = -1;
                    }
                }

                return address;
            }
        }

        public SourceMap SourceMap
        {
            get { return _sourceMap; }
        }

        public void SetSourceRoot(string sourceRoot)
        {
            _sourceRoot = sourceRoot;
        }

        public void AttachMap(SourceMap sourceMap)
        {
            _sourceMap = sourceMap;
        }

        /// <summary>
        /// Loads the appropriate source file, brings the specified line into view
        /// and highlights the specified line.
        /// </summary>
        /// <param name="entry"></param>
        public void SelectSourceEntry(SourceEntry entry, bool readOnly, bool iop)
        {
            LoadSourceFile(entry.SourcePath);
            SelectLine(entry.LineNumber);

            this.ReadOnly = readOnly;
            _iopCode = iop;
        }

        protected override void OnCurrentCellDirtyStateChanged(EventArgs e)
        {
            if (IsCurrentCellDirty)
            {
                //
                // Force checkbox changes to commit immediately (rather than
                // the default, which is to commit them when focus leaves the cell, which
                // is really annoying.
                //
                if (CurrentCell is DataGridViewCheckBoxCell)
                {
                    CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            }

            base.OnCurrentCellDirtyStateChanged(e);
        }

        protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex > _source.Count)
            {
                base.OnCellValueChanged(e);
                return;
            }

            switch (e.ColumnIndex)
            {
                case 0:     // breakpoint
                    {
                        // grab the check value
                        bool cellValue = (bool)this.Rows[e.RowIndex].Cells[e.ColumnIndex].EditedFormattedValue;

                        ushort address = 0;
                        bool addressAvailable = _sourceMap != null ? _sourceMap.GetAddressForSource(new SourceEntry(_currentSourceFile, new string[] { }, 0, e.RowIndex), out address) : false;

                        if (addressAvailable)
                        {
                            if (cellValue)
                            {
                                BreakpointManager.SetBreakpoint(new BreakpointEntry(_iopCode ? BreakpointProcessor.IOP : BreakpointProcessor.CP, BreakpointType.Execution, address));
                            }
                            else
                            {
                                BreakpointManager.SetBreakpoint(new BreakpointEntry(_iopCode ? BreakpointProcessor.IOP : BreakpointProcessor.CP, BreakpointType.None, address));
                            }
                        }
                    }
                    break;

                case 1:     // address
                    {
                        string oldCellValue = ((string)this.Rows[e.RowIndex].Cells[e.ColumnIndex].Value).Trim();

                        string[] symbolTokens = ((string)this.Rows[e.RowIndex].Cells[e.ColumnIndex].EditedFormattedValue).Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (symbolTokens.Length > 2)
                        {
                            MessageBox.Show("Invalid syntax.");
                            return;
                        }

                        if (oldCellValue.StartsWith("$"))
                        {
                            oldCellValue = oldCellValue.Substring(1);
                        }

                        if (symbolTokens.Length == 0)
                        {
                            // cell is empty, delete the current source map entry if present.
                            // TODO: use source map for this instead?

                            if (_sourceMap != null && !string.IsNullOrEmpty(oldCellValue))
                            {
                                ushort oldAddress = Convert.ToUInt16(oldCellValue, 16);
                                _sourceMap.RemoveSourceEntry(new SourceEntry(_currentSourceFile, new string[] { }, oldAddress, e.RowIndex));
                            }
                            return;
                        }

                        // 
                        // Valid new cell value.
                        //
                        string cellValue = symbolTokens[0];
                        string symbolName = symbolTokens.Length == 2 ? symbolTokens[1] : "*none*";

                        // strip leading $ if any.
                        if (cellValue.StartsWith("$"))
                        {
                            cellValue = cellValue.Substring(1);
                        }                       

                        try
                        {
                            ushort address = Convert.ToUInt16(cellValue, 16);
                            ushort oldAddress = string.IsNullOrWhiteSpace(oldCellValue) ? (ushort)0 : Convert.ToUInt16(oldCellValue, 16);

                            if (_sourceMap != null && address != oldAddress)
                            {
                                //
                                // Set the new value first.
                                //
                                _sourceMap.AddSourceEntry(new SourceEntry(_currentSourceFile, new string[] { symbolName }, address, e.RowIndex));

                                //
                                // Remove the old value from the database if there is one.
                                //
                                if (!string.IsNullOrWhiteSpace(oldCellValue))
                                {
                                    _sourceMap.RemoveSourceEntry(new SourceEntry(_currentSourceFile, new string[] { }, oldAddress, e.RowIndex));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Hey!");
                            // Invalid value, clear it.
                            // this.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = String.Empty;
                        }
                    }
                    break;
            }


            base.OnCellValueChanged(e);
        }      

        protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex > _source.Count)
            {
                // Past end of source, nothing to do.
                return;
            }

            switch (e.ColumnIndex)
            {
                case 0:
                    {
                        ushort address = 0;
                        bool addressAvailable = _sourceMap != null ? _sourceMap.GetAddressForSource(new SourceEntry(_currentSourceFile, new string[] { }, 0, e.RowIndex), out address) : false;
                        if (addressAvailable)
                        {
                            e.Value = BreakpointManager.GetBreakpoint(_iopCode ? BreakpointProcessor.IOP : BreakpointProcessor.CP, address) != BreakpointType.None;
                        }
                        else
                        {
                            e.Value = false;        // TODO: hook to breakpoint system 
                        }
                    }
                    break;

                case 1:
                    {
                        ushort address = 0;
                        bool addressAvailable = _sourceMap != null ? _sourceMap.GetAddressForSource(new SourceEntry(_currentSourceFile, new string[] { }, 0, e.RowIndex), out address) : false;
                        e.Value = addressAvailable ? String.Format("${0:x4}", address) : String.Empty;
                    }
                    break;

                case 2:
                    e.Value = _source[e.RowIndex];
                    break;

                default:
                    throw new InvalidOperationException("Unhandled column.");
            }

            base.OnCellValueNeeded(e);
        }

        private void LoadSourceFile(string sourcePath)
        {
            //
            // Only do this if the file isn't currently loaded.
            //
            try
            {
                if (_currentSourceFile != sourcePath)
                {
                    this.Invalidate();

                    _source = new List<string>();

                    using (StreamReader sr = new StreamReader(Path.Combine(_sourceRoot, sourcePath), Encoding.UTF8))
                    {
                        while (!sr.EndOfStream)
                        {
                            _source.Add(UnTabify(sr.ReadLine()));
                        }
                    }

                    this.RowCount = _source.Count;

                    _currentSourceFile = sourcePath;
                }
            }
            catch(Exception e)
            {
                _source = new List<string>();
                _source.Add(
                    String.Format("Unable to load source file {0}.  Error: {1}",
                    sourcePath, e.Message));
            }
        }

        /// <summary>
        /// Converts tabs in the given string to 4 space tabulation.  As it should be.
        /// </summary>
        /// <param name="tabified"></param>
        /// <returns></returns>
        private string UnTabify(string tabified)
        {
            StringBuilder untabified = new StringBuilder();

            int column = 0;

            foreach(char c in tabified)
            {
                if (c == '\t')
                {
                    untabified.Append(" ");
                    column++;
                    while ((column % 4) != 0)
                    {
                        untabified.Append(" ");
                        column++;
                    }
                }
                if (c == _unicodeUnknown)
                {
                    // TODO:
                    // We assume that if this happens it's the microcode source "arrow" symbol.
                    // C#'s StreamReader supports only Unicode/UTF and ASCII (7-bit) encodings and the
                    // Star's backarrow is an 8-bit character.  I should really just rewrite the code
                    // to read the bytes in myself, this is a bodge for the time being.
                    untabified.Append(_arrowChar);
                }
                else
                {
                    untabified.Append(c);
                    column++;
                }
            }

            return untabified.ToString();
        }

        private void SelectLine(int lineNumber)
        {
            //
            // Clear the current selection and move the selection to the first cell
            // of the requested line.
            //
            
            if (lineNumber < this.Rows.Count)
            {
                this.Rows[lineNumber].Selected = true;
                this.CurrentCell = this.Rows[lineNumber].Cells[0];
            }
            else
            {
                this.ClearSelection();
            }
        }

        private void AddColumn(string name, bool readOnly, DataGridViewAutoSizeColumnMode sizeMode)
        {
            int index = this.Columns.Add(name, name);
            
            this.Columns[index].ReadOnly = readOnly;
            this.Columns[index].Resizable = DataGridViewTriState.False;
            this.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
            this.Columns[index].AutoSizeMode = sizeMode;            
        }

        private void AddCheckboxColumn(string name, DataGridViewAutoSizeColumnMode sizeMode)
        {
            int index = this.Columns.Add(new DataGridViewCheckBoxColumn());

            this.Columns[index].HeaderText = name;
            this.Columns[index].ReadOnly = false;
            this.Columns[index].Resizable = DataGridViewTriState.False;
            this.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
            this.Columns[index].AutoSizeMode = sizeMode;
        }

        private string _currentSourceFile;
        private string _sourceRoot;
        private List<string> _source;
        private bool _iopCode;
        private SourceMap _sourceMap;

        //
        // Character substitutions
        //
        private const char _arrowChar = '←';
        private const char _unicodeUnknown = (char)0xfffd;

    }
}
