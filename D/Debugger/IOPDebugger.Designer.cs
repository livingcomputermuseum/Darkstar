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


namespace D.Debugger
{
    partial class IOPDebugger
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.IOPSourceBox = new System.Windows.Forms.GroupBox();
            this.SourceFiles = new System.Windows.Forms.ListBox();
            this.SourceDisplay = new D.Debugger.SourceDisplay();
            this.IOPSourceBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SourceDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // IOPSourceBox
            // 
            this.IOPSourceBox.Controls.Add(this.SourceFiles);
            this.IOPSourceBox.Controls.Add(this.SourceDisplay);
            this.IOPSourceBox.Location = new System.Drawing.Point(7, 3);
            this.IOPSourceBox.Name = "IOPSourceBox";
            this.IOPSourceBox.Size = new System.Drawing.Size(837, 725);
            this.IOPSourceBox.TabIndex = 2;
            this.IOPSourceBox.TabStop = false;
            this.IOPSourceBox.Text = "IOP Source";
            // 
            // SourceFiles
            // 
            this.SourceFiles.FormattingEnabled = true;
            this.SourceFiles.Location = new System.Drawing.Point(7, 20);
            this.SourceFiles.Name = "SourceFiles";
            this.SourceFiles.Size = new System.Drawing.Size(139, 693);
            this.SourceFiles.TabIndex = 1;
            this.SourceFiles.DoubleClick += new System.EventHandler(this.SourceFiles_DoubleClick);
            // 
            // SourceDisplay
            // 
            this.SourceDisplay.AllowUserToAddRows = false;
            this.SourceDisplay.AllowUserToDeleteRows = false;
            this.SourceDisplay.AllowUserToResizeColumns = false;
            this.SourceDisplay.AllowUserToResizeRows = false;
            this.SourceDisplay.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.SourceDisplay.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleVertical;
            this.SourceDisplay.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.NullValue = null;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SourceDisplay.DefaultCellStyle = dataGridViewCellStyle2;
            this.SourceDisplay.Location = new System.Drawing.Point(152, 19);
            this.SourceDisplay.Name = "SourceDisplay";
            this.SourceDisplay.RowHeadersVisible = false;
            this.SourceDisplay.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.SourceDisplay.RowTemplate.Height = 18;
            this.SourceDisplay.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.SourceDisplay.Size = new System.Drawing.Size(679, 694);
            this.SourceDisplay.TabIndex = 0;
            this.SourceDisplay.VirtualMode = true;
            // 
            // IOPDebugger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(851, 732);
            this.ControlBox = false;
            this.Controls.Add(this.IOPSourceBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IOPDebugger";
            this.Text = "IOP Debugger - MP {0}";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.IOPDebugger_FormClosed);
            this.IOPSourceBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SourceDisplay)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox IOPSourceBox;
        private SourceDisplay SourceDisplay;
        private System.Windows.Forms.ListBox SourceFiles;
    }
}