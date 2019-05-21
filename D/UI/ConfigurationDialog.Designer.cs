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


namespace D.UI
{
    partial class ConfigurationDialog
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
            this.TabControl = new System.Windows.Forms.TabControl();
            this.SystemPage = new System.Windows.Forms.TabPage();
            this.ThrottleSpeedCheckBox = new System.Windows.Forms.CheckBox();
            this.HostIDTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.MemorySizeComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.EthernetTab = new System.Windows.Forms.TabPage();
            this.EthernetInterfaceListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.DisplayTab = new System.Windows.Forms.TabPage();
            this.DisplayScaleComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SlowPhosphorCheckBox = new System.Windows.Forms.CheckBox();
            this.TimeTabPage = new System.Windows.Forms.TabPage();
            this.TODDatePicker = new System.Windows.Forms.DateTimePicker();
            this.SpecifiedDateRadioButton = new System.Windows.Forms.RadioButton();
            this.NoTimeDateChangeRadioButton = new System.Windows.Forms.RadioButton();
            this.TODDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.SpecifiedTimeDateRadioButton = new System.Windows.Forms.RadioButton();
            this.CurrentTimeDateY2KRadioButton = new System.Windows.Forms.RadioButton();
            this.CurrentTimeDateRadioButton = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.OKButton = new System.Windows.Forms.Button();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.FullScreenStretchCheckBox = new System.Windows.Forms.CheckBox();
            this.TabControl.SuspendLayout();
            this.SystemPage.SuspendLayout();
            this.EthernetTab.SuspendLayout();
            this.DisplayTab.SuspendLayout();
            this.TimeTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControl
            // 
            this.TabControl.Controls.Add(this.SystemPage);
            this.TabControl.Controls.Add(this.EthernetTab);
            this.TabControl.Controls.Add(this.DisplayTab);
            this.TabControl.Controls.Add(this.TimeTabPage);
            this.TabControl.Location = new System.Drawing.Point(7, 8);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(356, 195);
            this.TabControl.TabIndex = 0;
            // 
            // SystemPage
            // 
            this.SystemPage.Controls.Add(this.ThrottleSpeedCheckBox);
            this.SystemPage.Controls.Add(this.HostIDTextBox);
            this.SystemPage.Controls.Add(this.label2);
            this.SystemPage.Controls.Add(this.MemorySizeComboBox);
            this.SystemPage.Controls.Add(this.label1);
            this.SystemPage.Location = new System.Drawing.Point(4, 22);
            this.SystemPage.Name = "SystemPage";
            this.SystemPage.Padding = new System.Windows.Forms.Padding(3);
            this.SystemPage.Size = new System.Drawing.Size(348, 169);
            this.SystemPage.TabIndex = 0;
            this.SystemPage.Text = "System";
            this.SystemPage.UseVisualStyleBackColor = true;
            // 
            // ThrottleSpeedCheckBox
            // 
            this.ThrottleSpeedCheckBox.AutoSize = true;
            this.ThrottleSpeedCheckBox.Location = new System.Drawing.Point(9, 53);
            this.ThrottleSpeedCheckBox.Name = "ThrottleSpeedCheckBox";
            this.ThrottleSpeedCheckBox.Size = new System.Drawing.Size(146, 17);
            this.ThrottleSpeedCheckBox.TabIndex = 4;
            this.ThrottleSpeedCheckBox.Text = "Throttle Execution Speed";
            this.ThrottleSpeedCheckBox.UseVisualStyleBackColor = true;
            // 
            // HostIDTextBox
            // 
            this.HostIDTextBox.Location = new System.Drawing.Point(133, 27);
            this.HostIDTextBox.Name = "HostIDTextBox";
            this.HostIDTextBox.Size = new System.Drawing.Size(98, 20);
            this.HostIDTextBox.TabIndex = 3;
            this.HostIDTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.OnHostIDValidating);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Host ID (MAC Address):";
            // 
            // MemorySizeComboBox
            // 
            this.MemorySizeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MemorySizeComboBox.FormattingEnabled = true;
            this.MemorySizeComboBox.Items.AddRange(new object[] {
            "1024",
            "768",
            "512",
            "256",
            "128"});
            this.MemorySizeComboBox.Location = new System.Drawing.Point(110, 3);
            this.MemorySizeComboBox.Name = "MemorySizeComboBox";
            this.MemorySizeComboBox.Size = new System.Drawing.Size(121, 21);
            this.MemorySizeComboBox.TabIndex = 1;
            this.MemorySizeComboBox.SelectionChangeCommitted += new System.EventHandler(this.OnMemorySizeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Memory Size (KW):";
            // 
            // EthernetTab
            // 
            this.EthernetTab.Controls.Add(this.EthernetInterfaceListBox);
            this.EthernetTab.Controls.Add(this.label5);
            this.EthernetTab.Location = new System.Drawing.Point(4, 22);
            this.EthernetTab.Name = "EthernetTab";
            this.EthernetTab.Padding = new System.Windows.Forms.Padding(3);
            this.EthernetTab.Size = new System.Drawing.Size(348, 169);
            this.EthernetTab.TabIndex = 1;
            this.EthernetTab.Text = "Ethernet";
            this.EthernetTab.UseVisualStyleBackColor = true;
            // 
            // EthernetInterfaceListBox
            // 
            this.EthernetInterfaceListBox.FormattingEnabled = true;
            this.EthernetInterfaceListBox.Location = new System.Drawing.Point(10, 24);
            this.EthernetInterfaceListBox.Name = "EthernetInterfaceListBox";
            this.EthernetInterfaceListBox.Size = new System.Drawing.Size(332, 134);
            this.EthernetInterfaceListBox.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(240, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Select the network interface to use with Darkstar:";
            // 
            // DisplayTab
            // 
            this.DisplayTab.Controls.Add(this.FullScreenStretchCheckBox);
            this.DisplayTab.Controls.Add(this.DisplayScaleComboBox);
            this.DisplayTab.Controls.Add(this.label3);
            this.DisplayTab.Controls.Add(this.SlowPhosphorCheckBox);
            this.DisplayTab.Location = new System.Drawing.Point(4, 22);
            this.DisplayTab.Name = "DisplayTab";
            this.DisplayTab.Padding = new System.Windows.Forms.Padding(3);
            this.DisplayTab.Size = new System.Drawing.Size(348, 169);
            this.DisplayTab.TabIndex = 2;
            this.DisplayTab.Text = "Display";
            this.DisplayTab.UseVisualStyleBackColor = true;
            // 
            // DisplayScaleComboBox
            // 
            this.DisplayScaleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DisplayScaleComboBox.FormattingEnabled = true;
            this.DisplayScaleComboBox.Items.AddRange(new object[] {
            "1x",
            "2x",
            "4x"});
            this.DisplayScaleComboBox.Location = new System.Drawing.Point(84, 28);
            this.DisplayScaleComboBox.Name = "DisplayScaleComboBox";
            this.DisplayScaleComboBox.Size = new System.Drawing.Size(55, 21);
            this.DisplayScaleComboBox.TabIndex = 2;
            this.DisplayScaleComboBox.SelectionChangeCommitted += new System.EventHandler(this.OnDisplayScaleChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Display Scale";
            // 
            // SlowPhosphorCheckBox
            // 
            this.SlowPhosphorCheckBox.AutoSize = true;
            this.SlowPhosphorCheckBox.Location = new System.Drawing.Point(7, 7);
            this.SlowPhosphorCheckBox.Name = "SlowPhosphorCheckBox";
            this.SlowPhosphorCheckBox.Size = new System.Drawing.Size(148, 17);
            this.SlowPhosphorCheckBox.TabIndex = 0;
            this.SlowPhosphorCheckBox.Text = "Slow Phosphor Simulation";
            this.SlowPhosphorCheckBox.UseVisualStyleBackColor = true;
            // 
            // TimeTabPage
            // 
            this.TimeTabPage.Controls.Add(this.TODDatePicker);
            this.TimeTabPage.Controls.Add(this.SpecifiedDateRadioButton);
            this.TimeTabPage.Controls.Add(this.NoTimeDateChangeRadioButton);
            this.TimeTabPage.Controls.Add(this.TODDateTimePicker);
            this.TimeTabPage.Controls.Add(this.SpecifiedTimeDateRadioButton);
            this.TimeTabPage.Controls.Add(this.CurrentTimeDateY2KRadioButton);
            this.TimeTabPage.Controls.Add(this.CurrentTimeDateRadioButton);
            this.TimeTabPage.Controls.Add(this.label4);
            this.TimeTabPage.Location = new System.Drawing.Point(4, 22);
            this.TimeTabPage.Name = "TimeTabPage";
            this.TimeTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.TimeTabPage.Size = new System.Drawing.Size(348, 169);
            this.TimeTabPage.TabIndex = 3;
            this.TimeTabPage.Text = "Time";
            this.TimeTabPage.UseVisualStyleBackColor = true;
            // 
            // TODDatePicker
            // 
            this.TODDatePicker.CustomFormat = "MM\'/\'dd\'/\'yyyy";
            this.TODDatePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.TODDatePicker.Location = new System.Drawing.Point(136, 93);
            this.TODDatePicker.Name = "TODDatePicker";
            this.TODDatePicker.ShowUpDown = true;
            this.TODDatePicker.Size = new System.Drawing.Size(200, 20);
            this.TODDatePicker.TabIndex = 7;
            // 
            // SpecifiedDateRadioButton
            // 
            this.SpecifiedDateRadioButton.AutoSize = true;
            this.SpecifiedDateRadioButton.Location = new System.Drawing.Point(10, 93);
            this.SpecifiedDateRadioButton.Name = "SpecifiedDateRadioButton";
            this.SpecifiedDateRadioButton.Size = new System.Drawing.Size(118, 17);
            this.SpecifiedDateRadioButton.TabIndex = 6;
            this.SpecifiedDateRadioButton.TabStop = true;
            this.SpecifiedDateRadioButton.Text = "Specified date only:";
            this.SpecifiedDateRadioButton.UseVisualStyleBackColor = true;
            // 
            // NoTimeDateChangeRadioButton
            // 
            this.NoTimeDateChangeRadioButton.AutoSize = true;
            this.NoTimeDateChangeRadioButton.Location = new System.Drawing.Point(10, 119);
            this.NoTimeDateChangeRadioButton.Name = "NoTimeDateChangeRadioButton";
            this.NoTimeDateChangeRadioButton.Size = new System.Drawing.Size(205, 17);
            this.NoTimeDateChangeRadioButton.TabIndex = 5;
            this.NoTimeDateChangeRadioButton.TabStop = true;
            this.NoTimeDateChangeRadioButton.Text = "No change (do not modify TOD clock)";
            this.NoTimeDateChangeRadioButton.UseVisualStyleBackColor = true;
            // 
            // TODDateTimePicker
            // 
            this.TODDateTimePicker.CustomFormat = "MM\'/\'dd\'/\'yyyy HH\':\'mm\':\'ss";
            this.TODDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.TODDateTimePicker.Location = new System.Drawing.Point(136, 70);
            this.TODDateTimePicker.Name = "TODDateTimePicker";
            this.TODDateTimePicker.ShowUpDown = true;
            this.TODDateTimePicker.Size = new System.Drawing.Size(200, 20);
            this.TODDateTimePicker.TabIndex = 4;
            // 
            // SpecifiedTimeDateRadioButton
            // 
            this.SpecifiedTimeDateRadioButton.AutoSize = true;
            this.SpecifiedTimeDateRadioButton.Location = new System.Drawing.Point(10, 70);
            this.SpecifiedTimeDateRadioButton.Name = "SpecifiedTimeDateRadioButton";
            this.SpecifiedTimeDateRadioButton.Size = new System.Drawing.Size(120, 17);
            this.SpecifiedTimeDateRadioButton.TabIndex = 3;
            this.SpecifiedTimeDateRadioButton.TabStop = true;
            this.SpecifiedTimeDateRadioButton.Text = "Specified date/time:";
            this.SpecifiedTimeDateRadioButton.UseVisualStyleBackColor = true;
            // 
            // CurrentTimeDateY2KRadioButton
            // 
            this.CurrentTimeDateY2KRadioButton.AutoSize = true;
            this.CurrentTimeDateY2KRadioButton.Location = new System.Drawing.Point(10, 47);
            this.CurrentTimeDateY2KRadioButton.Name = "CurrentTimeDateY2KRadioButton";
            this.CurrentTimeDateY2KRadioButton.Size = new System.Drawing.Size(273, 17);
            this.CurrentTimeDateY2KRadioButton.TabIndex = 2;
            this.CurrentTimeDateY2KRadioButton.TabStop = true;
            this.CurrentTimeDateY2KRadioButton.Text = "Current time/date with Y2K compensation (-28 years)";
            this.CurrentTimeDateY2KRadioButton.UseVisualStyleBackColor = true;
            // 
            // CurrentTimeDateRadioButton
            // 
            this.CurrentTimeDateRadioButton.AutoSize = true;
            this.CurrentTimeDateRadioButton.Location = new System.Drawing.Point(10, 24);
            this.CurrentTimeDateRadioButton.Name = "CurrentTimeDateRadioButton";
            this.CurrentTimeDateRadioButton.Size = new System.Drawing.Size(107, 17);
            this.CurrentTimeDateRadioButton.TabIndex = 1;
            this.CurrentTimeDateRadioButton.TabStop = true;
            this.CurrentTimeDateRadioButton.Text = "Current time/date";
            this.CurrentTimeDateRadioButton.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(228, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "At power up/reset, set emulated TOD clock to:";
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(207, 209);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel_Button.Location = new System.Drawing.Point(288, 209);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(75, 23);
            this.Cancel_Button.TabIndex = 2;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            // 
            // FullScreenFilterCheckBox
            // 
            this.FullScreenStretchCheckBox.AutoSize = true;
            this.FullScreenStretchCheckBox.Location = new System.Drawing.Point(6, 55);
            this.FullScreenStretchCheckBox.Name = "FullScreenFilterCheckBox";
            this.FullScreenStretchCheckBox.Size = new System.Drawing.Size(186, 17);
            this.FullScreenStretchCheckBox.TabIndex = 3;
            this.FullScreenStretchCheckBox.Text = "Stretch screen in Fullscreen mode";
            this.FullScreenStretchCheckBox.UseVisualStyleBackColor = true;
            // 
            // ConfigurationDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 238);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.TabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "System Configuration";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnClosed);
            this.Load += new System.EventHandler(this.OnLoad);
            this.TabControl.ResumeLayout(false);
            this.SystemPage.ResumeLayout(false);
            this.SystemPage.PerformLayout();
            this.EthernetTab.ResumeLayout(false);
            this.EthernetTab.PerformLayout();
            this.DisplayTab.ResumeLayout(false);
            this.DisplayTab.PerformLayout();
            this.TimeTabPage.ResumeLayout(false);
            this.TimeTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage SystemPage;
        private System.Windows.Forms.TabPage EthernetTab;
        private System.Windows.Forms.TabPage DisplayTab;
        private System.Windows.Forms.ComboBox MemorySizeComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox HostIDTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox DisplayScaleComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox SlowPhosphorCheckBox;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button Cancel_Button;
        private System.Windows.Forms.CheckBox ThrottleSpeedCheckBox;
        private System.Windows.Forms.TabPage TimeTabPage;
        private System.Windows.Forms.DateTimePicker TODDateTimePicker;
        private System.Windows.Forms.RadioButton SpecifiedTimeDateRadioButton;
        private System.Windows.Forms.RadioButton CurrentTimeDateY2KRadioButton;
        private System.Windows.Forms.RadioButton CurrentTimeDateRadioButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox EthernetInterfaceListBox;
        private System.Windows.Forms.RadioButton NoTimeDateChangeRadioButton;
        private System.Windows.Forms.DateTimePicker TODDatePicker;
        private System.Windows.Forms.RadioButton SpecifiedDateRadioButton;
        private System.Windows.Forms.CheckBox FullScreenStretchCheckBox;
    }
}