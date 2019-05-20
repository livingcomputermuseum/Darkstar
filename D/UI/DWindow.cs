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


using D.Debugger;
using D.IO;
using D.IOP;
using D.Logging;
using SDL2;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace D.UI
{
    /// <summary>
    /// DWindow is the main user interface window for the emulator.
    /// The DWindow-IO file contains the portion of this class that implements the
    /// display, mouse, and keyboard.
    /// This file contains the UI-related code for controlling the emulation.    
    /// </summary>
    public partial class DWindow : Form
    {
        public DWindow(DSystem system)
        {
            _system = system;

            InitializeComponent();
            InitializeIO();

            PopulateAltBoot();            

            _frameTimer = new System.Windows.Forms.Timer();
            _frameTimer.Interval = 1000;
            _frameTimer.Tick += OnFrameTimerTick;
            _frameTimer.Start();

            _system.ExecutionStateChanged += OnExecutionStateChanged;
            _system.IOP.MiscIO.MPChanged += OnMPCodeChanged;

            //
            // Load any disks referenced by the configuration.
            //
            if (!string.IsNullOrWhiteSpace(Configuration.FloppyDriveImage))
            {
                try
                {
                    _system.IOP.FloppyController.Drive.LoadDisk(new FloppyDisk(Configuration.FloppyDriveImage));
                }
                catch(Exception e)
                {
                    Log.Write(LogType.Error, LogComponent.Configuration, "Unable to load floppy image {0}.  Error {1}",
                        Configuration.FloppyDriveImage,
                        e.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(Configuration.HardDriveImage))
            {
                try
                {
                    _system.HardDrive.Load(Configuration.HardDriveImage);
                }
                catch (Exception e)
                {
                    Log.Write(LogType.Error, LogComponent.Configuration, "Unable to load hard drive image {0}.  Error {1}",
                        Configuration.FloppyDriveImage,
                        e.Message);
                }
            }

            UpdateUIRunState();
            UpdateMPCode();
            UpdateHardDriveLabel();
            UpdateFloppyDriveLabel();
            UpdateMouseState();            
        }

        //
        // UI Event handlers:
        //
        private void StartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_system.IsExecuting)
            {
                SystemExecutionContext context = new SystemExecutionContext(null, null, null, OnExecutionError);
                _system.StartExecution(context);
            }
            else
            {
                _system.StopExecution();
            }
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _system.Reset();
        }

        private void FloppyLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string imagePath = ShowImageLoadDialog(true);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                _system.IOP.FloppyController.Drive.LoadDisk(new FloppyDisk(imagePath));
                Configuration.FloppyDriveImage = imagePath;

                UpdateFloppyDriveLabel();                
            }
        }

        private void FloppyUnloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _system.IOP.FloppyController.Drive.UnloadDisk();
            Configuration.FloppyDriveImage = String.Empty;
            UpdateFloppyDriveLabel();
        }

        private void LoadHardDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string imagePath = ShowImageLoadDialog(false);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                //
                // Timer to update progress bar while loading:
                //
                Timer t = new Timer();
                t.Interval = 100;
                t.Enabled = true;
                t.Tick += (o, i) =>
                {
                    if (ProgressBar.Value >= ProgressBar.Maximum)
                    {
                        ProgressBar.Value = 0;
                    }
                    ProgressBar.Increment(10);
                };
                
                //
                // Do load asynchronously.  UI is disabled during this time.
                //
                System.Threading.ThreadPool.QueueUserWorkItem(
                delegate
                {                    
                    BeginInvoke((MethodInvoker)delegate
                    {
                        //
                        // Set up status bar
                        //
                        ProgressBar.Visible = true;
                        ProgressBar.Style = ProgressBarStyle.Blocks;
                        ProgressBar.Value = 0;
                        ProgressBar.Minimum = 0;
                        ProgressBar.Maximum = 100;

                        //
                        // Disable UI
                        //
                        SystemMenu.Enabled = false;
                    });

                    //
                    // Pause the system if it's running.
                    //
                    bool isRunning = _system.IsExecuting;
                    SystemExecutionContext context = _system.ExecutionContext;

                    if (isRunning)
                    {
                        _system.StopExecution();
                    }

                    DialogResult res = DialogResult.Yes;
                    try
                    {
                        // Commit current image to disk.
                        _system.HardDrive.Save();
                    }
                    catch (Exception ex)
                    {
                        res = MessageBox.Show(
                            String.Format("Unable to save current hard drive's contents.  Error: {0}.  Continue loading new drive image?", ex.Message),
                            "Error:",
                            MessageBoxButtons.YesNo);
                    }

                    if (res == DialogResult.Yes)
                    {

                        try
                        {
                            // Load new image.
                            _system.HardDrive.Load(imagePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                String.Format("Unable to load drive image.  Error: {0}", ex.Message),
                                "Error:");
                        }

                        Configuration.HardDriveImage = imagePath;
                    }

                    //
                    // Restart the system if necessary
                    //
                    if (isRunning)
                    {
                        _system.StartExecution(context);
                    }

                    //
                    // Hide status text, re-enable UI
                    //
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        t.Stop();
                        ProgressBar.Visible = false;
                        SystemMenu.Enabled = true;

                        UpdateHardDriveLabel();
                    });
                    
                });
            }
        }

        private void NewHardDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Ask for the new image's path.
            string imagePath = ShowNewImageDialog(false);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                D.IO.DriveType type = (D.IO.DriveType)((ToolStripMenuItem)sender).Tag;
                _system.HardDrive.NewDisk(type, imagePath);

                UpdateHardDriveLabel();
            }
        }

        private void OnAltBootOptionClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            _system.IOP.MiscIO.AltBoot = (AltBootValues)item.Tag;

            // Uncheck all items, check the selected one.
            foreach(ToolStripMenuItem m in AlternateBootToolStripMenuItem.DropDownItems)
            {
                m.Checked = false;
            }

            item.Checked = true;
        }

        private void ConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigurationDialog configDialog = new ConfigurationDialog();
            configDialog.MemorySize = Configuration.MemorySize;
            configDialog.HostID = Configuration.HostID;
            configDialog.HostPacketInterfaceName = Configuration.HostPacketInterfaceName;
            configDialog.ThrottleSpeed = Configuration.ThrottleSpeed;
            configDialog.DisplayScale = Configuration.DisplayScale;
            configDialog.SlowPhosphor = Configuration.SlowPhosphor;
            configDialog.TODDateTime = Configuration.TODDateTime;
            configDialog.TODDate = Configuration.TODDate;
            configDialog.TODSetMode = Configuration.TODSetMode;

            DialogResult res = configDialog.ShowDialog(this);

            if (res == DialogResult.OK)
            {
                if (_system.IsExecuting &&
                    configDialog.MemorySize != Configuration.MemorySize)
                {
                    MessageBox.Show("Changes to system memory size will not take effect until the system is restarted.");
                }

                if (configDialog.HostID != Configuration.HostID)
                {
                    Configuration.HostID = configDialog.HostID;

                    // Break substitution.
                    ((IOPMemoryBus)_system.IOP.Memory).UpdateHostIDProm();
                }

                if (configDialog.HostPacketInterfaceName != Configuration.HostPacketInterfaceName)
                {
                    Configuration.HostPacketInterfaceName = configDialog.HostPacketInterfaceName;
                    _system.EthernetController.HostInterfaceChanged();
                }

                if (configDialog.TODSetMode != Configuration.TODSetMode ||
                    configDialog.TODDateTime != Configuration.TODDateTime ||
                    configDialog.TODDate != Configuration.TODDate)
                {
                    _system.IOP.MiscIO.TODClock.PowerUpSetMode = configDialog.TODSetMode;
                    _system.IOP.MiscIO.TODClock.PowerUpSetTime = 
                        (configDialog.TODSetMode == TODPowerUpSetMode.SpecificDateAndTime) ? 
                            configDialog.TODDateTime : configDialog.TODDate;
                    _system.IOP.MiscIO.TODClock.ResetTODClockTime();

                    if (_system.IsExecuting)
                    {
                        MessageBox.Show("Changes to system time may not take effect until the system is restarted.");
                    }
                }                

                Configuration.MemorySize = configDialog.MemorySize;                
                Configuration.ThrottleSpeed = configDialog.ThrottleSpeed;
                Configuration.DisplayScale = configDialog.DisplayScale;
                Configuration.SlowPhosphor = configDialog.SlowPhosphor;
                Configuration.TODDateTime = configDialog.TODDateTime;
                Configuration.TODDate = configDialog.TODDate;
                Configuration.TODSetMode = configDialog.TODSetMode;

                UpdateDisplayScale();
                UpdateSlowPhosphor();
            }
        }

        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleFullScreen(true);
        }

        private void ShowDebuggerToolStripMenu_Click(object sender, EventArgs e)
        {
            if (_debuggerWindow == null)
            {
                _debuggerWindow = new DebuggerMain(
                    _system, 
                    DebuggerReason.UserInvoked, 
                    "*** Welcome to the Debugger! ***");
                _debuggerWindow.Show(this);
                _debuggerWindow.FormClosed += OnDebuggerWindowClosed;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ViewDocumentationToolStripMenu_Click(object sender, EventArgs e)
        {
            Process.Start(_readmeFilename);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog(this);
        }

        private void OnDebuggerWindowClosed(object sender, FormClosedEventArgs e)
        {            
            _debuggerWindow = null;

            //
            // Reattach our context if the system is currently running.
            //
            if (_system.IsExecuting)
            {
                _system.StopExecution();

                SystemExecutionContext context = new SystemExecutionContext(null, null, null, OnExecutionError);
                _system.StartExecution(context);
            }
        }

        private void OnWindowClosed(object sender, FormClosedEventArgs e)
        {
            //
            // Ensure emulation is stopped.
            //
            _system.StopExecution();

            //
            // Stop the SDL thread.
            //            
            SDL.SDL_Event closeEvent = new SDL.SDL_Event();
            closeEvent.type = SDL.SDL_EventType.SDL_QUIT;
            SDL.SDL_PushEvent(ref closeEvent);

            _sdlThread.Join(1000);

            //
            // Commit config back to storage.
            //
            Configuration.WriteConfiguration();

            DialogResult = DialogResult.OK;
        }       

        private void OnFrameTimerTick(object sender, EventArgs e)
        {
            FPSStatusLabel.Text = String.Format("{0} Fields/Sec ({1}%)", 
                _frameCount, 
                (int)((_frameCount / 77.4) * 100.0));

            _frameCount = 0;
        }

        private void OnExecutionError(Exception e)
        {
            if (_debuggerWindow == null)
            {
                _debuggerWindow = new DebuggerMain(
                    _system,
                    DebuggerReason.Error,
                    String.Format("*** Execution error: {0} ***", e.Message));
                BeginInvoke(new DisplayDelegate(DisplayDebugger));
            }
        }

        private void OnMPCodeChanged()
        {
            BeginInvoke(new DisplayDelegate(UpdateMPCode));
        }

        private void OnExecutionStateChanged()
        {
            BeginInvoke(new DisplayDelegate(UpdateUIRunState));
        }

        private void DisplayDebugger()
        {
            _debuggerWindow.Show(this);
            _debuggerWindow.FormClosed += OnDebuggerWindowClosed;
        }

        private void UpdateMPCode()
        {
            if (_system.IOP.MiscIO.MPanelBlank)
            {
                MPStatusLabel.Text = "MP: ----";
            }
            else
            {
                MPStatusLabel.Text = String.Format("MP: {0:d4}", _system.IOP.MiscIO.MPanelValue);
            }
        }       

        private void UpdateUIRunState()
        {
            StartToolStripMenuItem.Text = _system.IsExecuting ? "Stop" : "Start";
            ExecutionStatusLabel.Text = _system.IsExecuting ? "System is running." : "System is stopped.";
        }

        private void UpdateMouseState()
        {
            MouseCaptureStatusLabel.Text = _mouseCaptured ?
                "Mouse captured.  Press Alt to release." :
                "Click on the display to capture mouse/keyboard.";
        }

        private void UpdateHardDriveLabel()
        {
            HardDiskLabelToolStripMenuItem.Text = 
                String.Format("{0} ({1})", 
                    Path.GetFileName(_system.HardDrive.ImagePath),
                    _system.HardDrive.Type);

            HardDiskLabelToolStripMenuItem.ToolTipText = _system.HardDrive.ImagePath;
        }

        private void UpdateFloppyDriveLabel()
        {                    
            FloppyLabelToolStripMenuItem.Text =
                _system.IOP.FloppyController.Drive.IsLoaded ?
                    Path.GetFileName(_system.IOP.FloppyController.Drive.Disk.ImagePath) :
                    "No Floppy Loaded";

            FloppyLabelToolStripMenuItem.ToolTipText = _system.IOP.FloppyController.Drive.IsLoaded ?
                String.Format("{0}\r\n{1}",
                    _system.IOP.FloppyController.Drive.Disk.ImagePath,
                    _system.IOP.FloppyController.Drive.Disk.Description) :
                    "No Floppy Loaded";
        }

        private void PopulateAltBoot()
        {
            for (AltBootValues v = AltBootValues.None; v < AltBootValues.HeadCleaning; v++)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(v.ToString());
                item.Tag = (object)v;
                item.Click += OnAltBootOptionClick;
                
                // Check the item if it's the current alt boot value
                item.Checked = (v == _system.IOP.MiscIO.AltBoot);

                AlternateBootToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private string ShowImageLoadDialog(bool floppy)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.DefaultExt = floppy ? "imd" : "img";
            fileDialog.Filter = floppy ? _floppyDiskFilter : _hardDiskFilter;
            fileDialog.Multiselect = false;
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Title = String.Format("Select disk image to load.");

            DialogResult res = fileDialog.ShowDialog();

            if (res == DialogResult.OK)
            {
                return fileDialog.FileName;
            }
            else
            {
                return null;
            }
        }

        private string ShowNewImageDialog(bool floppy)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();

            fileDialog.DefaultExt = floppy ? "imd" : "img";
            fileDialog.Filter = floppy ? _floppyDiskFilter : _hardDiskFilter;
            fileDialog.OverwritePrompt = true;
            fileDialog.ValidateNames = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Title = String.Format("Select path for new disk image.");

            DialogResult res = fileDialog.ShowDialog();

            if (res == DialogResult.OK)
            {
                return fileDialog.FileName;
            }
            else
            {
                return null;
            }
        }

        //
        // Timer for FPS display
        //
        private System.Windows.Forms.Timer _frameTimer;

        //
        // Debugger window reference
        //
        private DebuggerMain _debuggerWindow;

        //
        // TODO: Move to string resources
        //
        private const string _hardDiskFilter = "Star Hard Disk Images (*.img)|*.img|All Files(*.*)|*.*";
        private const string _floppyDiskFilter = "Star Floppy Disk Images (*.imd)|*.imd|All Files(*.*)|*.*";
        private const string _readmeFilename = "readme.txt";
    }
}
