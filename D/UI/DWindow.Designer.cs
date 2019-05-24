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
    partial class DWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DWindow));
            this.SystemMenu = new System.Windows.Forms.MenuStrip();
            this.SystemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ResetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AlternateBootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.floppyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FloppyLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FloppyUnloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FloppyLabelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HardDiskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LoadHardDiskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewHardDiskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewSA1004ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewQ2040ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewQ2080ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HardDiskLabelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ConfigurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showDebuggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UIPanel = new System.Windows.Forms.Panel();
            this.DisplayBox = new System.Windows.Forms.Panel();
            this.SystemStatus = new System.Windows.Forms.StatusStrip();
            this.MPStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ExecutionStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FPSStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.MouseCaptureStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.FloppyWriteProtectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SystemMenu.SuspendLayout();
            this.UIPanel.SuspendLayout();
            this.SystemStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // SystemMenu
            // 
            this.SystemMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.SystemMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SystemToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.SystemMenu.Location = new System.Drawing.Point(0, 0);
            this.SystemMenu.Name = "SystemMenu";
            this.SystemMenu.Size = new System.Drawing.Size(1113, 24);
            this.SystemMenu.TabIndex = 1;
            this.SystemMenu.Text = "System Menu";
            // 
            // SystemToolStripMenuItem
            // 
            this.SystemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StartToolStripMenuItem,
            this.ResetToolStripMenuItem,
            this.AlternateBootToolStripMenuItem,
            this.floppyToolStripMenuItem,
            this.HardDiskToolStripMenuItem,
            this.ConfigurationToolStripMenuItem,
            this.FullScreenToolStripMenuItem,
            this.showDebuggerToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.SystemToolStripMenuItem.Name = "SystemToolStripMenuItem";
            this.SystemToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.SystemToolStripMenuItem.Text = "System";
            // 
            // StartToolStripMenuItem
            // 
            this.StartToolStripMenuItem.Name = "StartToolStripMenuItem";
            this.StartToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.StartToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.StartToolStripMenuItem.Text = "Start";
            this.StartToolStripMenuItem.Click += new System.EventHandler(this.StartToolStripMenuItem_Click);
            // 
            // ResetToolStripMenuItem
            // 
            this.ResetToolStripMenuItem.Name = "ResetToolStripMenuItem";
            this.ResetToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.R)));
            this.ResetToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.ResetToolStripMenuItem.Text = "Reset";
            this.ResetToolStripMenuItem.Click += new System.EventHandler(this.ResetToolStripMenuItem_Click);
            // 
            // AlternateBootToolStripMenuItem
            // 
            this.AlternateBootToolStripMenuItem.Name = "AlternateBootToolStripMenuItem";
            this.AlternateBootToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.AlternateBootToolStripMenuItem.Text = "Alternate Boot";
            // 
            // floppyToolStripMenuItem
            // 
            this.floppyToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FloppyLoadToolStripMenuItem,
            this.FloppyUnloadToolStripMenuItem,
            this.FloppyWriteProtectToolStripMenuItem,
            this.FloppyLabelToolStripMenuItem});
            this.floppyToolStripMenuItem.Name = "floppyToolStripMenuItem";
            this.floppyToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.floppyToolStripMenuItem.Text = "Floppy Disk";
            // 
            // FloppyLoadToolStripMenuItem
            // 
            this.FloppyLoadToolStripMenuItem.Name = "FloppyLoadToolStripMenuItem";
            this.FloppyLoadToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.D)));
            this.FloppyLoadToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.FloppyLoadToolStripMenuItem.Text = "Load...";
            this.FloppyLoadToolStripMenuItem.Click += new System.EventHandler(this.FloppyLoadToolStripMenuItem_Click);
            // 
            // FloppyUnloadToolStripMenuItem
            // 
            this.FloppyUnloadToolStripMenuItem.Name = "FloppyUnloadToolStripMenuItem";
            this.FloppyUnloadToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.FloppyUnloadToolStripMenuItem.Text = "Unload";
            this.FloppyUnloadToolStripMenuItem.Click += new System.EventHandler(this.FloppyUnloadToolStripMenuItem_Click);
            // 
            // FloppyLabelToolStripMenuItem
            // 
            this.FloppyLabelToolStripMenuItem.Enabled = false;
            this.FloppyLabelToolStripMenuItem.Name = "FloppyLabelToolStripMenuItem";
            this.FloppyLabelToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.FloppyLabelToolStripMenuItem.Text = "No Floppy Loaded";
            // 
            // HardDiskToolStripMenuItem
            // 
            this.HardDiskToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadHardDiskToolStripMenuItem,
            this.NewHardDiskToolStripMenuItem,
            this.HardDiskLabelToolStripMenuItem});
            this.HardDiskToolStripMenuItem.Name = "HardDiskToolStripMenuItem";
            this.HardDiskToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.HardDiskToolStripMenuItem.Text = "Hard Disk";
            // 
            // LoadHardDiskToolStripMenuItem
            // 
            this.LoadHardDiskToolStripMenuItem.Name = "LoadHardDiskToolStripMenuItem";
            this.LoadHardDiskToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.H)));
            this.LoadHardDiskToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.LoadHardDiskToolStripMenuItem.Text = "Load...";
            this.LoadHardDiskToolStripMenuItem.Click += new System.EventHandler(this.LoadHardDiskToolStripMenuItem_Click);
            // 
            // NewHardDiskToolStripMenuItem
            // 
            this.NewHardDiskToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSA1004ToolStripMenuItem,
            this.NewQ2040ToolStripMenuItem,
            this.NewQ2080ToolStripMenuItem});
            this.NewHardDiskToolStripMenuItem.Name = "NewHardDiskToolStripMenuItem";
            this.NewHardDiskToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.NewHardDiskToolStripMenuItem.Text = "New";
            // 
            // NewSA1004ToolStripMenuItem
            // 
            this.NewSA1004ToolStripMenuItem.Name = "NewSA1004ToolStripMenuItem";
            this.NewSA1004ToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.NewSA1004ToolStripMenuItem.Tag = 1;
            this.NewSA1004ToolStripMenuItem.Text = "10MB (SA1004)";
            this.NewSA1004ToolStripMenuItem.Click += new System.EventHandler(this.NewHardDiskToolStripMenuItem_Click);
            // 
            // NewQ2040ToolStripMenuItem
            // 
            this.NewQ2040ToolStripMenuItem.Name = "NewQ2040ToolStripMenuItem";
            this.NewQ2040ToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.NewQ2040ToolStripMenuItem.Tag = 2;
            this.NewQ2040ToolStripMenuItem.Text = "40MB (Q2040)";
            this.NewQ2040ToolStripMenuItem.Click += new System.EventHandler(this.NewHardDiskToolStripMenuItem_Click);
            // 
            // NewQ2080ToolStripMenuItem
            // 
            this.NewQ2080ToolStripMenuItem.Name = "NewQ2080ToolStripMenuItem";
            this.NewQ2080ToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.NewQ2080ToolStripMenuItem.Tag = 3;
            this.NewQ2080ToolStripMenuItem.Text = "80MB (Q2080)";
            this.NewQ2080ToolStripMenuItem.Click += new System.EventHandler(this.NewHardDiskToolStripMenuItem_Click);
            // 
            // HardDiskLabelToolStripMenuItem
            // 
            this.HardDiskLabelToolStripMenuItem.Enabled = false;
            this.HardDiskLabelToolStripMenuItem.Name = "HardDiskLabelToolStripMenuItem";
            this.HardDiskLabelToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.HardDiskLabelToolStripMenuItem.Text = "No Hard Drive Loaded";
            // 
            // ConfigurationToolStripMenuItem
            // 
            this.ConfigurationToolStripMenuItem.Name = "ConfigurationToolStripMenuItem";
            this.ConfigurationToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.C)));
            this.ConfigurationToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.ConfigurationToolStripMenuItem.Text = "Configuration...";
            this.ConfigurationToolStripMenuItem.Click += new System.EventHandler(this.ConfigurationToolStripMenuItem_Click);
            // 
            // FullScreenToolStripMenuItem
            // 
            this.FullScreenToolStripMenuItem.Name = "FullScreenToolStripMenuItem";
            this.FullScreenToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
            this.FullScreenToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.FullScreenToolStripMenuItem.Text = "Full Screen";
            this.FullScreenToolStripMenuItem.Click += new System.EventHandler(this.FullScreenToolStripMenuItem_Click);
            // 
            // showDebuggerToolStripMenuItem
            // 
            this.showDebuggerToolStripMenuItem.Name = "showDebuggerToolStripMenuItem";
            this.showDebuggerToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.showDebuggerToolStripMenuItem.Text = "Show Debugger";
            this.showDebuggerToolStripMenuItem.Click += new System.EventHandler(this.ShowDebuggerToolStripMenu_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.ExitToolStripMenuItem.Text = "Exit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ViewDocumentationToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // ViewDocumentationToolStripMenuItem
            // 
            this.ViewDocumentationToolStripMenuItem.Name = "ViewDocumentationToolStripMenuItem";
            this.ViewDocumentationToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.ViewDocumentationToolStripMenuItem.Text = "View Documentation...";
            this.ViewDocumentationToolStripMenuItem.Click += new System.EventHandler(this.ViewDocumentationToolStripMenu_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // UIPanel
            // 
            this.UIPanel.AutoSize = true;
            this.UIPanel.Controls.Add(this.DisplayBox);
            this.UIPanel.Controls.Add(this.SystemStatus);
            this.UIPanel.Location = new System.Drawing.Point(0, 27);
            this.UIPanel.Name = "UIPanel";
            this.UIPanel.Size = new System.Drawing.Size(711, 323);
            this.UIPanel.TabIndex = 2;
            // 
            // DisplayBox
            // 
            this.DisplayBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayBox.Location = new System.Drawing.Point(0, 0);
            this.DisplayBox.Name = "DisplayBox";
            this.DisplayBox.Size = new System.Drawing.Size(711, 299);
            this.DisplayBox.TabIndex = 3;
            this.DisplayBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnWinformsMouseDown);
            this.DisplayBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnWinformsMouseMove);
            this.DisplayBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnWinformsMouseUp);
            // 
            // SystemStatus
            // 
            this.SystemStatus.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.SystemStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MPStatusLabel,
            this.ExecutionStatusLabel,
            this.FPSStatusLabel,
            this.MouseCaptureStatusLabel,
            this.ProgressBar});
            this.SystemStatus.Location = new System.Drawing.Point(0, 299);
            this.SystemStatus.Name = "SystemStatus";
            this.SystemStatus.Size = new System.Drawing.Size(711, 24);
            this.SystemStatus.SizingGrip = false;
            this.SystemStatus.TabIndex = 2;
            this.SystemStatus.Text = "System Status";
            // 
            // MPStatusLabel
            // 
            this.MPStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.MPStatusLabel.Name = "MPStatusLabel";
            this.MPStatusLabel.Size = new System.Drawing.Size(94, 19);
            this.MPStatusLabel.Text = "MP Placeholder";
            // 
            // ExecutionStatusLabel
            // 
            this.ExecutionStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ExecutionStatusLabel.Name = "ExecutionStatusLabel";
            this.ExecutionStatusLabel.Size = new System.Drawing.Size(108, 19);
            this.ExecutionStatusLabel.Text = "Status Placeholder";
            // 
            // FPSStatusLabel
            // 
            this.FPSStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.FPSStatusLabel.Name = "FPSStatusLabel";
            this.FPSStatusLabel.Size = new System.Drawing.Size(95, 19);
            this.FPSStatusLabel.Text = "FPS Placeholder";
            // 
            // MouseCaptureStatusLabel
            // 
            this.MouseCaptureStatusLabel.Name = "MouseCaptureStatusLabel";
            this.MouseCaptureStatusLabel.Size = new System.Drawing.Size(108, 19);
            this.MouseCaptureStatusLabel.Text = "Mouse Placeholder";
            // 
            // ProgressBar
            // 
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(100, 18);
            this.ProgressBar.Visible = false;
            // 
            // FloppyWriteProtectToolStripMenuItem
            // 
            this.FloppyWriteProtectToolStripMenuItem.Name = "FloppyWriteProtectToolStripMenuItem";
            this.FloppyWriteProtectToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.FloppyWriteProtectToolStripMenuItem.Text = "Write Protect";
            this.FloppyWriteProtectToolStripMenuItem.Click += new System.EventHandler(this.FloppyWriteProtectToolStripMenuItem_Click);
            // 
            // DWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1113, 972);
            this.Controls.Add(this.UIPanel);
            this.Controls.Add(this.SystemMenu);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.SystemMenu;
            this.MaximizeBox = false;
            this.Name = "DWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Darkstar";
            this.Deactivate += new System.EventHandler(this.OnWindowDeactivate);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnWindowClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
            this.Leave += new System.EventHandler(this.OnWindowLeave);
            this.SystemMenu.ResumeLayout(false);
            this.SystemMenu.PerformLayout();
            this.UIPanel.ResumeLayout(false);
            this.UIPanel.PerformLayout();
            this.SystemStatus.ResumeLayout(false);
            this.SystemStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip SystemMenu;
        private System.Windows.Forms.ToolStripMenuItem SystemToolStripMenuItem;
        private System.Windows.Forms.Panel UIPanel;
        private System.Windows.Forms.StatusStrip SystemStatus;
        private System.Windows.Forms.ToolStripStatusLabel ExecutionStatusLabel;
        private System.Windows.Forms.Panel DisplayBox;
        private System.Windows.Forms.ToolStripMenuItem floppyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FloppyLoadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FloppyUnloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HardDiskToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem StartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ResetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoadHardDiskToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewHardDiskToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConfigurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AlternateBootToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel MPStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel FPSStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem showDebuggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel MouseCaptureStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem FloppyLabelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HardDiskLabelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewSA1004ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewQ2040ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewQ2080ToolStripMenuItem;
        private System.Windows.Forms.ToolStripProgressBar ProgressBar;
        private System.Windows.Forms.ToolStripMenuItem ViewDocumentationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FullScreenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FloppyWriteProtectToolStripMenuItem;
    }
}