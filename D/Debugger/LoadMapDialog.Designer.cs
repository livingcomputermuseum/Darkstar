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
    partial class LoadMapDialog
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CurrentLoadsList = new System.Windows.Forms.ListBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.LoadNameText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.LoadStartBox = new System.Windows.Forms.TextBox();
            this.LoadEndBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.CreateLoadButton = new System.Windows.Forms.Button();
            this.CancelCreateButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.CancelAddButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.CancelAddButton);
            this.groupBox1.Controls.Add(this.AddButton);
            this.groupBox1.Controls.Add(this.CurrentLoadsList);
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(171, 137);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Add to Existing Microcode Load";
            // 
            // CurrentLoadsList
            // 
            this.CurrentLoadsList.FormattingEnabled = true;
            this.CurrentLoadsList.Location = new System.Drawing.Point(7, 16);
            this.CurrentLoadsList.Name = "CurrentLoadsList";
            this.CurrentLoadsList.Size = new System.Drawing.Size(156, 82);
            this.CurrentLoadsList.TabIndex = 0;
            this.CurrentLoadsList.SelectedIndexChanged += new System.EventHandler(this.OnSelectionChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.CancelCreateButton);
            this.groupBox2.Controls.Add(this.CreateLoadButton);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.LoadEndBox);
            this.groupBox2.Controls.Add(this.LoadStartBox);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.LoadNameText);
            this.groupBox2.Location = new System.Drawing.Point(182, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(178, 137);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Add to New Microcode Load";
            // 
            // LoadNameText
            // 
            this.LoadNameText.Location = new System.Drawing.Point(51, 17);
            this.LoadNameText.Name = "LoadNameText";
            this.LoadNameText.Size = new System.Drawing.Size(103, 20);
            this.LoadNameText.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name:";
            // 
            // LoadStartBox
            // 
            this.LoadStartBox.Location = new System.Drawing.Point(51, 44);
            this.LoadStartBox.Name = "LoadStartBox";
            this.LoadStartBox.Size = new System.Drawing.Size(100, 20);
            this.LoadStartBox.TabIndex = 2;
            // 
            // LoadEndBox
            // 
            this.LoadEndBox.Location = new System.Drawing.Point(51, 71);
            this.LoadEndBox.Name = "LoadEndBox";
            this.LoadEndBox.Size = new System.Drawing.Size(100, 20);
            this.LoadEndBox.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Start:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "End:";
            // 
            // CreateLoadButton
            // 
            this.CreateLoadButton.Location = new System.Drawing.Point(13, 104);
            this.CreateLoadButton.Name = "CreateLoadButton";
            this.CreateLoadButton.Size = new System.Drawing.Size(75, 23);
            this.CreateLoadButton.TabIndex = 6;
            this.CreateLoadButton.Text = "Create";
            this.CreateLoadButton.UseVisualStyleBackColor = true;
            this.CreateLoadButton.Click += new System.EventHandler(this.CreateLoadButton_Click);
            // 
            // CancelCreateButton
            // 
            this.CancelCreateButton.Location = new System.Drawing.Point(94, 104);
            this.CancelCreateButton.Name = "CancelCreateButton";
            this.CancelCreateButton.Size = new System.Drawing.Size(75, 23);
            this.CancelCreateButton.TabIndex = 7;
            this.CancelCreateButton.Text = "Cancel";
            this.CancelCreateButton.UseVisualStyleBackColor = true;
            this.CancelCreateButton.Click += new System.EventHandler(this.CancelCreateButton_Click);
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(7, 104);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 7;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // CancelAddButton
            // 
            this.CancelAddButton.Location = new System.Drawing.Point(88, 104);
            this.CancelAddButton.Name = "CancelAddButton";
            this.CancelAddButton.Size = new System.Drawing.Size(75, 23);
            this.CancelAddButton.TabIndex = 8;
            this.CancelAddButton.Text = "Cancel";
            this.CancelAddButton.UseVisualStyleBackColor = true;
            this.CancelAddButton.Click += new System.EventHandler(this.CancelAddButton_Click);
            // 
            // LoadMapDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 146);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "LoadMapDialog";
            this.Text = "Select Microcode Load For File";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button CancelAddButton;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.ListBox CurrentLoadsList;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button CancelCreateButton;
        private System.Windows.Forms.Button CreateLoadButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox LoadEndBox;
        private System.Windows.Forms.TextBox LoadStartBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LoadNameText;
    }
}