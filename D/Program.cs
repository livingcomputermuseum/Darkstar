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
using System.Windows.Forms;
using D.UI;

namespace D
{
    public static class StartupOptions
    {
        public static string ConfigurationFile;

        public static string RomPath;

    }
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
        	bool doStart = false;
            //
            // Check for command-line arguments.
            //            
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i++].ToLowerInvariant())
                    {
                        case "-config":
                            if (i < args.Length)
                            {
                                StartupOptions.ConfigurationFile = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-rompath":
                            if (i < args.Length)
                            {
                                StartupOptions.RomPath = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-start":
                            doStart = true;
                            break;
                            
                        default:
                            PrintUsage();
                            return;
                    }
                }
            }

            PrintHerald();

            // Cons up a system to run stuff on.
            DSystem system = new DSystem();
            system.Reset();
            Configuration.Start |= doStart;

            //
            // Start the UI, this will not return from ShowDialog
            // until the window is closed.
            //
            DWindow mainWindow = new DWindow(system);
            system.AttachDisplay(mainWindow);
            DialogResult res = mainWindow.ShowDialog();
            
            //
            // Main window is now closed: shut down the system.
            // Ensure the system is stopped.
            //
            system.StopExecution();

            //
            // Commit disks on normal exit
            //
            system.Shutdown(res == DialogResult.OK);
            
            Console.WriteLine("Goodbye...");
        }        
        
        private static void PrintHerald()
        {
            Console.WriteLine("Darkstar v{0}", typeof(Program).Assembly.GetName().Version);
            Console.WriteLine("(c) 2017-2019 Living Computers: Museum+Labs");
            Console.WriteLine("Bug reports to joshd@livingcomputers.org");
            Console.WriteLine();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: Darkstar [-config <configurationFile>] [-rompath <path>]");
        }
    }
}
