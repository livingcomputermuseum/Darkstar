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


using D.IOP;
using D.Logging;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace D
{ 
    public enum PlatformType
    {
        Windows,
        Unix
    }    

    /// <summary>
    /// Encapsulates user-configurable settings.  To be enhanced.
    /// </summary>
    public static class Configuration
    {
        static Configuration()
        {
            // Initialize things to defaults.
            MemorySize = 768;
            HostID = 0x0000aa012345;
            ThrottleSpeed = true;
            DisplayScale = 1;
            SlowPhosphor = true;
            HostPacketInterfaceName = String.Empty;

            TODDateTime = new DateTime(1979, 12, 10);
            TODDate = new DateTime(1955, 11, 5);
            TODSetMode = TODPowerUpSetMode.HostTimeY2K;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    Platform = PlatformType.Unix;
                    break;

                default:
                    Platform = PlatformType.Windows;
                    break;
            }

            // See if PCap is available.
            TestPCap();

            try
            {
                ReadConfiguration();
            }
            catch
            {
                Log.Write(LogType.Error, LogComponent.Configuration, 
                    "Unable to load configuration.  Assuming default settings.");
            }

            //
            // Sanity-check settings that need sanity-checking.
            //
            if (MemorySize > 768 ||
                MemorySize == 0 ||
                (MemorySize % 128) != 0)
            {
                Log.Write(LogType.Error, LogComponent.Configuration,
                    "MemorySize configuration parameter is incorrect, defaulting to 768KW.");

                MemorySize = 768;
            }

            if (DisplayScale < 1)
            {
                Log.Write(LogType.Error, LogComponent.Configuration,
                    "DisplayScale configuration parameter is incorrect, defaulting to 1.");

                DisplayScale = 1;
            }
        }

        /// <summary>
        /// What kind of system we're running on.  (Not technically configurable.)
        /// </summary>
        public static PlatformType Platform;

        /// <summary>
        /// System memory size, in KW.
        /// </summary>
        public static uint MemorySize;

        /// <summary>
        /// The currently loaded image for the hard disk.
        /// </summary>
        public static string HardDriveImage;

        /// <summary>
        /// The currently loaded image for the floppy drive.
        /// </summary>
        public static string FloppyDriveImage;

        /// <summary>
        /// The Ethernet host address for this Star.
        /// </summary>
        public static ulong HostID;

        /// <summary>
        /// The name of the Ethernet adaptor on the emulator host to use for Ethernet emulation
        /// </summary>
        public static string HostPacketInterfaceName;

        /// <summary>
        /// Whether any packet interfaces are available on the host
        /// </summary>
        public static bool HostRawEthernetInterfacesAvailable;       

        /// <summary>
        /// Whether to cap execution speed at native execution speed or not.
        /// </summary>
        public static bool ThrottleSpeed;

        /// <summary>
        /// Scale factor to apply to the display.
        /// </summary>
        public static uint DisplayScale;

        /// <summary>
        /// Whether to apply a fake "slow phosphor" persistence to the emulated display.
        /// </summary>
        public static bool SlowPhosphor;

        /// <summary>
        /// How to set the TOD clock at power up/reset
        /// </summary>
        public static TODPowerUpSetMode TODSetMode;

        /// <summary>
        /// The specific date/time to set the TOD clock to if TODSetMode is "SpecificDateAndTime"
        /// </summary>
        public static DateTime TODDateTime;

        /// <summary>
        /// The specific date to set the TOD clock to if TODSetMode is "SpecificDate"
        /// </summary>
        public static DateTime TODDate;

        /// <summary>
        /// The components to enable debug logging for.
        /// </summary>
        public static LogComponent LogComponents;

        /// <summary>
        /// The types of logging to enable.
        /// </summary>
        public static LogType LogTypes;        

        /// <summary>
        /// Reads the current configuration file from the appropriate place.
        /// </summary>
        public static void ReadConfiguration()
        {
            if (Configuration.Platform == PlatformType.Windows)
                // && string.IsNullOrWhiteSpace(StartupOptions.ConfigurationFile))
            {
                //
                // By default, on Windows we use the app Settings functionality
                // to store settings in the registry on a per-user basis.
                // If a configuration file is specified, we will use it instead.
                //
                ReadConfigurationWindows();
            }
            else
            {
                //
                // On UNIX platforms we read in a configuration file.
                // This is mostly because Mono's support for Properties.Settings
                // is broken in inexplicable ways and I'm tired of fighting with it.
                //
                ReadConfigurationUnix();
            }
        }

        /// <summary>
        /// Commits the current configuration to the app's settings.
        /// </summary>
        public static void WriteConfiguration()
        {
            if (Configuration.Platform == PlatformType.Windows)
            {
                WriteConfigurationWindows();
            }
            else
            {
                //
                // At the moment the configuration files are read-only
                // on UNIX platforms.
                //
            }
        }

        private static void ReadConfigurationWindows()
        {
            MemorySize = Properties.Settings.Default.MemorySize;
            HardDriveImage = Properties.Settings.Default.HardDriveImage;
            FloppyDriveImage = Properties.Settings.Default.FloppyDriveImage;         
            HostID = Properties.Settings.Default.HostAddress;
            HostPacketInterfaceName = Properties.Settings.Default.HostPacketInterfaceName;            
            ThrottleSpeed = Properties.Settings.Default.ThrottleSpeed;
            DisplayScale = Properties.Settings.Default.DisplayScale;
            SlowPhosphor = Properties.Settings.Default.SlowPhosphor;
            TODSetMode = (TODPowerUpSetMode)Properties.Settings.Default.TODSetMode;
            TODDateTime = Properties.Settings.Default.TODDateTime;
            TODDate = Properties.Settings.Default.TODDate;
        }

        private static void WriteConfigurationWindows()
        {
            Properties.Settings.Default.MemorySize = MemorySize;
            Properties.Settings.Default.HardDriveImage = HardDriveImage;
            Properties.Settings.Default.FloppyDriveImage = FloppyDriveImage;            
            Properties.Settings.Default.HostAddress = HostID;
            Properties.Settings.Default.HostPacketInterfaceName = HostPacketInterfaceName;            
            Properties.Settings.Default.ThrottleSpeed = ThrottleSpeed;
            Properties.Settings.Default.DisplayScale = DisplayScale;
            Properties.Settings.Default.SlowPhosphor = SlowPhosphor;
            Properties.Settings.Default.TODSetMode = (int)TODSetMode;
            Properties.Settings.Default.TODDateTime = TODDateTime;
            Properties.Settings.Default.TODDate = TODDate;
            Properties.Settings.Default.Save();
        }

        private static void ReadConfigurationUnix()
        {
            string configFilePath = null;

            if (false) //!string.IsNullOrWhiteSpace(StartupOptions.ConfigurationFile))
            {
                // configFilePath = StartupOptions.ConfigurationFile;
            }
            else
            {
                // No config file specified, default.
                configFilePath = "Darkstar.cfg";
            }

            //
            // Check that the configuration file exists.
            // If not, we will warn the user and use default settings.
            //
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("Configuration file {0} does not exist or cannot be accessed.  Using default settings.", configFilePath);
                return;
            }

            using (StreamReader configStream = new StreamReader(configFilePath))
            {
                //
                // Config file consists of text lines containing name / value pairs:
                //      <Name>=<Value>
                // Whitespace is ignored.
                //
                int lineNumber = 0;
                while (!configStream.EndOfStream)
                {
                    lineNumber++;
                    string line = configStream.ReadLine().Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        // Empty line, ignore.
                        continue;
                    }

                    if (line.StartsWith("#"))
                    {
                        // Comment to EOL, ignore.
                        continue;
                    }

                    // Find the '=' separating tokens and ensure there are just two.
                    string[] tokens = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length < 2)
                    {
                        Console.WriteLine(
                            "{0} line {1}: Invalid syntax.", configFilePath, lineNumber);
                        continue;
                    }

                    string parameter = tokens[0].Trim();
                    string value = tokens[1].Trim();

                    // Reflect over the public, static properties in this class and see if the parameter matches one of them
                    // If not, it's an error, if it is then we attempt to coerce the value to the correct type.
                    System.Reflection.FieldInfo[] info = typeof(Configuration).GetFields(BindingFlags.Public | BindingFlags.Static);

                    bool bMatch = false;
                    foreach (FieldInfo field in info)
                    {
                        // Case-insensitive compare.
                        if (field.Name.ToLowerInvariant() == parameter.ToLowerInvariant())
                        {
                            bMatch = true;

                            //
                            // Switch on the type of the field and attempt to convert the value to the appropriate type.
                            // At this time we support only strings and integers.
                            //
                            try
                            {
                                switch (field.FieldType.Name)
                                {
                                    case "Int32":
                                        {
                                            int v = Convert.ToInt32(value, 10);
                                            field.SetValue(null, v);
                                        }
                                        break;

                                    case "UInt16":
                                        {
                                            UInt16 v = Convert.ToUInt16(value, 16);
                                            field.SetValue(null, v);
                                        }
                                        break;

                                    case "Byte":
                                        {
                                            byte v = Convert.ToByte(value, 16);
                                            field.SetValue(null, v);
                                        }
                                        break;

                                    case "String":
                                        {
                                            field.SetValue(null, value);
                                        }
                                        break;

                                    case "Boolean":
                                        {
                                            bool v = bool.Parse(value);
                                            field.SetValue(null, v);
                                        }
                                        break;

                                    case "UInt64":
                                        {
                                            UInt64 v = Convert.ToUInt64(value, 16);
                                            field.SetValue(null, v);
                                        }
                                        break;

                                    case "TODPowerUpSetMode":
                                        {
                                            field.SetValue(null, Enum.Parse(typeof(TODPowerUpSetMode), value, true));
                                        }
                                        break;

                                    case "DateTime":
                                        {
                                            field.SetValue(null, DateTime.Parse(value));
                                        }
                                        break;

                                    case "LogType":
                                        {
                                            field.SetValue(null, Enum.Parse(typeof(LogType), value, true));
                                        }
                                        break;

                                    case "LogComponent":
                                        {
                                            field.SetValue(null, Enum.Parse(typeof(LogComponent), value, true));
                                        }
                                        break;

                                    case "StringCollection":
                                        {
                                            // value is expected to be a comma-delimited set.
                                            StringCollection sc = new StringCollection();
                                            string[] strings = value.Split(',');

                                            foreach (string s in strings)
                                            {
                                                sc.Add(s);
                                            }

                                            field.SetValue(null, sc);
                                        }
                                        break;
                                }
                            }
                            catch
                            {
                                Console.WriteLine(
                                    "{0} line {1}: Value '{2}' is invalid for parameter '{3}'.", configFilePath, lineNumber, value, parameter);
                            }
                        }
                    }

                    if (!bMatch)
                    {
                        Console.WriteLine(
                            "{0} line {1}: Unknown configuration parameter '{2}'.", configFilePath, lineNumber, parameter);
                    }
                }
            }
        }

        private static void TestPCap()
        {
            // Just try enumerating interfaces, if this fails for any reason we assume
            // PCap is not properly installed.            
            try
            {
                SharpPcap.CaptureDeviceList devices = SharpPcap.CaptureDeviceList.Instance;
                Configuration.HostRawEthernetInterfacesAvailable = true;
            }
            catch
            {
                Configuration.HostRawEthernetInterfacesAvailable = false;
            }            
        }


    }

}
