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


using D.CP;
using System;
using System.Windows.Forms;

namespace D.Debugger
{
    /// <summary>
    /// Presents a view of microcode disassembly, with addresses and breakpoints.
    /// 
    /// MicrocodeDisplay provides a three-column view:
    /// 
    /// | Breakpoint | Address | Disassembly                                    |
    /// -------------------------------------------------------------------------
    /// 
    /// Breakpoint is editable (to allow setting breakpoints)
    /// 
    /// TODO: This is pretty similar in functionality to the IOP source view.  I'm
    /// sure there's some code that could be shared...
    /// 
    /// </summary>
    public class MicrocodeDisplay : DataGridView
    {
        public MicrocodeDisplay()
        {
            this.VirtualMode = true;
            this.RowHeadersVisible = false;
            this.ReadOnly = false;

            AddCheckboxColumn("B", DataGridViewAutoSizeColumnMode.ColumnHeader);
            AddColumn("Address", true, DataGridViewAutoSizeColumnMode.ColumnHeader);
            AddColumn("Disassembly", true, DataGridViewAutoSizeColumnMode.Fill);
        }
        
        /// <summary>
        /// Returns the address selected in the disassembly.  Since the row index and the 
        /// address are identical, we just return the selected row index (if any).
        /// Returns -1 if nothing is selected.
        /// </summary>
        public int SelectedAddress
        {
            get { return this.SelectedCells.Count > 0 ? this.SelectedCells[0].RowIndex : -1; }
        }

        public void AttachCP(CentralProcessor cp)
        {
            _cp = cp;

            this.RowCount = cp.MicrocodeRam.Length;
        }

        public void SelectAddress(int address)
        {
            if (address < 0 || address > _cp.MicrocodeRam.Length)
            {
                throw new InvalidOperationException("Invalid address.");
            }

            //
            // Clear the current selection and move the selection to the first cell
            // of the requested line.
            //
            this.ClearSelection();
            this.Rows[address].Selected = true;
            this.CurrentCell = this.Rows[address].Cells[0];
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
            if (e.RowIndex < 0 || e.RowIndex > _cp.MicrocodeRam.Length)
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
                        
                        if (cellValue)
                        {
                            BreakpointManager.SetBreakpoint(new BreakpointEntry(BreakpointProcessor.CP, BreakpointType.Execution, (ushort)e.RowIndex));
                        }
                        else
                        {
                            BreakpointManager.SetBreakpoint(new BreakpointEntry(BreakpointProcessor.CP, BreakpointType.None, (ushort)e.RowIndex));
                        }
                        
                    }
                    break;
            }

            base.OnCellValueChanged(e);
        }

        protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex > _cp.MicrocodeRam.Length)
            {
                // Past end of microcode, nothing to do.
                base.OnCellValueNeeded(e);
                return;
            }

            switch (e.ColumnIndex)
            {
                case 0:
                    {
                        ushort address = (ushort)e.RowIndex;
                        
                        e.Value = BreakpointManager.GetBreakpoint(BreakpointProcessor.CP, address) != BreakpointType.None;
                    }
                    break;

                case 1:                    
                    e.Value = String.Format("{0:x3}", e.RowIndex);                    
                    break;

                case 2:
                    e.Value = new Microinstruction(_cp.MicrocodeRam[e.RowIndex]).Disassemble(-1);    
                    break;

                default:
                    throw new InvalidOperationException("Unhandled column.");
            }

            base.OnCellValueNeeded(e);
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

        private CentralProcessor _cp;
    }
}
