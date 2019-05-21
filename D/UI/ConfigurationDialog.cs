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


using D.Ethernet;
using D.IOP;
using SharpPcap;
using SharpPcap.WinPcap;
using System;
using System.Windows.Forms;

namespace D.UI
{
    public partial class ConfigurationDialog : Form
    {
        public ConfigurationDialog()
        {
            InitializeComponent();
        }

        public uint MemorySize;
        public ulong HostID;
        public bool ThrottleSpeed;

        public string HostPacketInterfaceName;

        public bool SlowPhosphor;
        public uint DisplayScale;
        public bool FullScreenStretch;

        public TODPowerUpSetMode TODSetMode;
        public DateTime TODDateTime;
        public DateTime TODDate;

        private void PopulateUI()
        {
            // System Tab
            MemorySizeComboBox.Items.Clear();
            for (int i = 128; i <= 768; i+= 128)
            {
                MemorySizeComboBox.Items.Add(string.Format("{0}KW", i));
                if (i == MemorySize)
                {
                    MemorySizeComboBox.SelectedIndex = MemorySizeComboBox.Items.Count - 1;
                }
            }

            HostIDTextBox.Text = string.Format("{0:x12}", HostID);
            ThrottleSpeedCheckBox.Checked = ThrottleSpeed;

            // Ethernet Tab
            PopulateNetworkAdapterList();

            // Display Tab
            DisplayScaleComboBox.Items.Clear();
            for (int i = 1; i < 5; i++)
            {
                DisplayScaleComboBox.Items.Add(string.Format("{0}x", i));
                if (i == DisplayScale)
                {
                    DisplayScaleComboBox.SelectedIndex = i - 1;
                }
            }
            
            SlowPhosphorCheckBox.Checked = SlowPhosphor;
            FullScreenStretchCheckBox.Checked = FullScreenStretch;

            // Time Tab
            switch (TODSetMode)
            {
                case TODPowerUpSetMode.HostTime:
                    CurrentTimeDateRadioButton.Checked = true;
                    break;

                case TODPowerUpSetMode.HostTimeY2K:
                    CurrentTimeDateY2KRadioButton.Checked = true;
                    break;

                case TODPowerUpSetMode.SpecificDateAndTime:
                    SpecifiedTimeDateRadioButton.Checked = true;
                    break;

                case TODPowerUpSetMode.SpecificDate:
                    SpecifiedDateRadioButton.Checked = true;
                    break;

                case TODPowerUpSetMode.NoChange:
                    NoTimeDateChangeRadioButton.Checked = true;
                    break;
            }

            TODDateTimePicker.Value = TODDateTime;
            TODDatePicker.Value = TODDate;
        }

        private void PopulateNetworkAdapterList()
        {
            //
            // Populate the list with the interfaces available on the machine, if any.
            //
            EthernetInterfaceListBox.Enabled = Configuration.HostRawEthernetInterfacesAvailable;
            EthernetInterfaceListBox.Items.Clear();


            // Add the "Use no interface" option
            EthernetInterfaceListBox.Items.Add(
                new EthernetInterface("None", "No network adapter"));
           
            // Add all interfaces that PCAP knows about.
            
            if (Configuration.HostRawEthernetInterfacesAvailable)
            {
                if (Configuration.Platform == PlatformType.Windows)
                {
                    foreach (WinPcapDevice device in CaptureDeviceList.Instance)
                    {
                        if (!string.IsNullOrWhiteSpace(device.Interface.FriendlyName))
                        {
                            EthernetInterfaceListBox.Items.Add(
                                new EthernetInterface(device.Interface.FriendlyName, device.Interface.Description));
                        }
                    }
                }
                else
                {
                    foreach (SharpPcap.LibPcap.LibPcapLiveDevice device in CaptureDeviceList.Instance)
                    {
                        EthernetInterfaceListBox.Items.Add(
                            new EthernetInterface(device.Interface.FriendlyName, device.Interface.Description));
                    }
                }
            }
           
            //
            // Select the one that is already selected (if any)
            //
            EthernetInterfaceListBox.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(Configuration.HostPacketInterfaceName))
            {
                for (int i = 0; i < EthernetInterfaceListBox.Items.Count; i++)
                {
                    EthernetInterface iface = (EthernetInterface)EthernetInterfaceListBox.Items[i];

                    if (iface.Name == HostPacketInterfaceName)
                    {
                        EthernetInterfaceListBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void OnLoad(object sender, System.EventArgs e)
        {
            PopulateUI();
        }

        private void OnMemorySizeChanged(object sender, EventArgs e)
        {
            MemorySize = (uint)((MemorySizeComboBox.SelectedIndex + 1) * 128);
        }

        private void OnHostIDValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //
            // Validate the input -- remove spaces, colons, etc.
            //
            string stripped = HostIDTextBox.Text.Replace(":", string.Empty);
            stripped = stripped.Replace(" ", string.Empty);

            try
            {
                ulong newValue = Convert.ToUInt64(stripped, 16);

                // Ensure the value is in range
                if ((newValue & 0xffff0000000000) != 0)
                {
                    throw new ArgumentOutOfRangeException("HostID");
                }

                HostID = newValue;
            }
            catch
            {
                //
                // Any failure, we reset back to default.
                //
                MessageBox.Show("Invalid value for Host ID");
                HostIDTextBox.Text = string.Format("{0:x12}", HostID);
            }
        }

        private void OnDisplayScaleChanged(object sender, EventArgs e)
        {
            DisplayScale = (uint)(DisplayScaleComboBox.SelectedIndex + 1);
        }

        private void OnClosed(object sender, FormClosedEventArgs e)
        {
            ThrottleSpeed = ThrottleSpeedCheckBox.Checked;
            SlowPhosphor = SlowPhosphorCheckBox.Checked;
            FullScreenStretch = FullScreenStretchCheckBox.Checked;

            HostPacketInterfaceName = ((EthernetInterface)EthernetInterfaceListBox.SelectedItem).Name;

            if (CurrentTimeDateRadioButton.Checked)
            {
                TODSetMode = TODPowerUpSetMode.HostTime;
            }
            else if (CurrentTimeDateY2KRadioButton.Checked)
            {
                TODSetMode = TODPowerUpSetMode.HostTimeY2K;
            }
            else if (SpecifiedTimeDateRadioButton.Checked)
            {
                TODSetMode = TODPowerUpSetMode.SpecificDateAndTime;
                TODDateTime = TODDateTimePicker.Value;
            }
            else if (SpecifiedDateRadioButton.Checked)
            {
                TODSetMode = TODPowerUpSetMode.SpecificDate;
                TODDate = TODDatePicker.Value;
            }
            else if (NoTimeDateChangeRadioButton.Checked)
            {
                TODSetMode = TODPowerUpSetMode.NoChange;
            }
        }
    }
}
