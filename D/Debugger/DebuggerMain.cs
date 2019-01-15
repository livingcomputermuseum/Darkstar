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
using D.IO;
using D.IOP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace D.Debugger
{
    public enum DebuggerReason
    {
        UserInvoked,
        Error,
    }

    public partial class DebuggerMain : Form
    {
        public DebuggerMain(DSystem system, DebuggerReason reason, string message)
        {
            InitializeComponent();

            _reason = reason;
            _entryMessage = message;
            _system = system;
        }

        protected override void OnLoad(EventArgs e)
        {
            //
            // Pop up some debugger source windows.
            //            
            _iopDebugger = new IOPDebugger(_system);
            _iopDebugger.Show();

            _cpDebugger = new CPDebugger(_system);
            _cpDebugger.Show();

            this.BringToFront();

            switch(_reason)
            {
                case DebuggerReason.UserInvoked:
                    WriteLine(_entryMessage);
                    break;

                case DebuggerReason.Error:
                    WriteLine(_entryMessage);
                    PrintIOPStatus();
                    PrintCPStatus();
                    PrintMesaStatus();
                    break;
            }

            base.OnLoad(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // We just intercept Ctrl+C here
            if (e.Control && e.KeyCode == Keys.C && _system.IsExecuting)
            {
                WriteLine("*user break*");
                StopExecution();
                DisplayCurrentCode();
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private void DebuggerInput_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                WriteLine(String.Format("> {0}", DebuggerInput.Text));

                try
                {
                    ExecuteCommand(DebuggerInput.Text);
                }
                catch(Exception ex)
                {
                    WriteLine(ex.Message);
                }

                // Clear input line for next input
                DebuggerInput.Text = String.Empty;
            }
        }

        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                command = _lastCommand;
            }

            string[] tokens = command.Trim().Split(' ');

            if (tokens.Length == 0)
            {
                // Nothing to do
                return;
            }

            switch (tokens[0].ToLowerInvariant())
            {
                case ".":   // redisplay status
                    DisplayCurrentCode();
                    PrintIOPStatus();
                    PrintCPStatus();
                    break;

                case "i":   // single step IOP
                    StartExecution(true, false, false);
                    StopExecution();  // wait for exec to finish
                    DisplayCurrentCode();
                    PrintIOPStatus();
                    break;

                case "s":   // single step CP
                    _stepCount = 0;
                    StartExecution(false, true, false);
                    StopExecution();  // wait for exec to finish
                    DisplayCurrentCode();
                    PrintCPStatus();
                    break;

                case "m":   // single step Mesa macrocode
                    StartExecution(false, false, true);
                    StopExecution();  // wait for exec to finish
                    DisplayCurrentCode();
                    PrintMesaStatus();
                    break;

                case "g":   // run system
                    StartExecution(false, false, false /* normal execution */);
                    break;

                case "r":   // reset system
                    _system.Reset();
                    WriteLine("System reset.");
                    break;

                case "tpc": // print TPC registers
                    DisplayTPCRegisters();
                    break;

                case "task": // select Task to debug (skips over microcode execution for other tasks while single-stepping)
                    switch (tokens.Length)
                    {
                        case 2:
                            if (tokens[1].ToLowerInvariant() != "all")
                            {
                                // Step through a specific task only.
                                _debugTask = (TaskType)Enum.Parse(typeof(TaskType), tokens[1], true);
                                _debugSpecificTask = true;
                            }
                            else
                            {
                                // Step through all tasks
                                _debugSpecificTask = false;
                            }
                            break;

                        default:
                            WriteLine("task <task type>");
                            break;
                    }
                    break;

                case "id":  // dump IOP memory
                    switch(tokens.Length)
                    {
                        case 2:
                            DumpMemory(Convert.ToUInt16(tokens[1], 16), 1, null);
                            break;

                        case 3:
                            DumpMemory(Convert.ToUInt16(tokens[1], 16), Convert.ToUInt16(tokens[2], 16), null);
                            break;

                        case 4:
                            DumpMemory(Convert.ToUInt16(tokens[1], 16), Convert.ToUInt16(tokens[2], 16), tokens[3]);
                            break;

                        default:
                            WriteLine("id <address> [count] [file]");
                            break;
                    }
                    break;

                case "cd":  // dump CP memory
                    switch (tokens.Length)
                    {
                        case 2:
                            DumpCPMemory(Convert.ToUInt32(tokens[1], 16), 1, null);
                            break;

                        case 3:
                            DumpCPMemory(Convert.ToUInt32(tokens[1], 16), Convert.ToUInt16(tokens[2], 16), null);
                            break;

                        case 4:
                            DumpCPMemory(Convert.ToUInt32(tokens[1], 16), Convert.ToUInt16(tokens[2], 16), tokens[3]);
                            break;

                        default:
                            WriteLine("cd <address> [count] [file]");
                            break;
                    }
                    break;

                case "fd":  // dump floppy sector
                    switch(tokens.Length)
                    {
                        case 4:
                            DumpFloppySector(Convert.ToUInt16(tokens[1], 10),
                            Convert.ToUInt16(tokens[2], 10),
                            Convert.ToUInt16(tokens[3], 10),
                            null);
                            break;

                        case 5:
                            DumpFloppySector(Convert.ToUInt16(tokens[1], 10),
                            Convert.ToUInt16(tokens[2], 10),
                            Convert.ToUInt16(tokens[3], 10),
                            tokens[4]);
                            break;

                        default:
                            WriteLine("id <track> <head> <sector> [file]");
                            break;
                    }                    
                    break;

                case "u":  // dump U register
                    switch (tokens.Length)
                    {
                        case 2:
                            DumpURegister(Convert.ToUInt16(tokens[1], 16));
                            break;

                        default:
                            WriteLine("u <register>");
                            break;
                    }
                    break;

                case "mapv": // Map virtual address to physical
                    switch (tokens.Length)
                    {
                        case 2:
                            TranslateVirtualAddress(Convert.ToInt32(tokens[1], 16));
                            break;

                        default:
                            WriteLine("mapv <vaddress>");
                            break;
                    }
                    break;

                case "mapp": // Map physical address to virtual
                    switch (tokens.Length)
                    {
                        case 2:
                            TranslatePhysicalAddress(Convert.ToInt32(tokens[1], 16));
                            break;

                        default:
                            WriteLine("mapp <paddress>");
                            break;
                    }
                    break;

                case "mapd": // Dump map entry
                    switch (tokens.Length)
                    {
                        case 2:
                            DumpMapEntry(Convert.ToInt32(tokens[1], 16));
                            break;

                        default:
                            WriteLine("mapd <map address>");
                            break;
                    }
                    break;

                case "mbs": // Set Mesa breakpoint
                    switch (tokens.Length)
                    {
                        case 2:
                            SetMesaBreakpoint(Convert.ToInt32(tokens[1], 16));
                            break;

                        default:
                            WriteLine("mbs <mesa instruction address>");
                            break;
                    }
                    break;

                case "mbc": // Clear Mesa breakpoint
                    switch (tokens.Length)
                    {
                        case 2:
                            ClearMesaBreakpoint(Convert.ToInt32(tokens[1], 16));
                            break;

                        default:
                            WriteLine("mbs <mesa instruction address>");
                            break;
                    }
                    break;

                case "da":    // analyze disk
                    AnalyzeDisk();
                    break;

                case "dt":    // he can do stupid things
                    switch (tokens.Length)
                    {
                        case 3:
                            DumpTrack(Convert.ToInt32(tokens[1], 10), Convert.ToInt32(tokens[2], 10));
                            break;

                        default:
                            WriteLine("dt <cylinder> <head>");
                            break;
                    }
                    break;


                case "save":  // commit source annotations
                    _iopDebugger.Save();
                    _cpDebugger.Save();
                    WriteLine("Source annotations saved.");
                    break;

                case "clear":
                    DebugOutput.Clear();
                    break;

                case "?":
                case "help":
                    DisplayHelp();
                    break;

                default:
                    WriteLine("?");     // for DMR
                    break;
            }

            _lastCommand = command;
        }       

        private void StartExecution(bool singleStepIOP, bool singleStepCP, bool singleStepMesa)
        {
            _singleStepIOP = singleStepIOP;
            _singleStepCP = singleStepCP;
            _singleStepMesa = singleStepMesa;

            SystemExecutionContext context =
                new SystemExecutionContext(StepCallback8085, StepCallbackCP, StepCallbackMesa, ErrorCallback);
            _system.StartExecution(context);

            if (!_singleStepCP && !_singleStepIOP && !_singleStepMesa)
            {
                WriteLine("System started.");
            }
        }

        private void StopExecution()
        {
            _system.StopExecution();
            WriteLine("System stopped.");
        }

        private bool StepCallback8085()
        {
            bool stopExecution = _singleStepIOP;

            // Check for execution breakpoints
            if (BreakpointManager.TestBreakpoint(BreakpointProcessor.IOP, BreakpointType.Execution, _system.IOP.CPU.PC))
            {
                BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("* IOP Execution breakpoint hit at PC=${0:x4} *", _system.IOP.CPU.PC));
                stopExecution = true;
            }

            // IOP processor still running?
            if (_system.IOP.CPU.Halted)
            {
                BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("* 8085 halted at PC=${0:x4} *", _system.IOP.CPU.PC));
                stopExecution = true;
            }

            return stopExecution;
        }

        private bool StepCallbackCP()
        {
            bool stopExecution = false;

            if (!_debugSpecificTask)
            {
                // Stop after every step
                stopExecution = _singleStepCP;
            }
            else
            {
                //
                // Stop only if the current task is the task we're debugging or we've exhausted our cycle count
                // waiting for the task to wake up again.
                //
                _stepCount++;

                stopExecution = _singleStepCP && (_system.CP.CurrentTask == _debugTask || _stepCount > 1000);

                if (_stepCount > 1000)
                {
                    BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("Timeout waiting for task {0} to wake.", _debugTask));
                }
            }

            if (!_system.CP.IOPWait)
            {
                // Check for execution breakpoints
                int tpc = _system.CP.TPC[(int)_system.CP.CurrentTask];
                if (BreakpointManager.TestBreakpoint(BreakpointProcessor.CP, BreakpointType.Execution, (ushort)tpc))
                {
                    BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("* CP Execution breakpoint hit at TPC=0x{0:x3} *", tpc));
                    stopExecution = true;
                }
            }

            return stopExecution;
        }

        private bool StepCallbackMesa()
        {
            bool stopExecution = _singleStepMesa;
            
            if (!_system.CP.IOPWait)
            {
                // Check for execution breakpoints
                int mesaPC = ((((_system.CP.RH[5] & 0xf) << 16) | _system.CP.ALU.R[5]) << 1) | (_system.CP.PC16 ? 1 : 0);
                if (BreakpointManager.TestBreakpoint(BreakpointProcessor.Mesa, BreakpointType.Execution, mesaPC))
                {
                    BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("* Mesa Execution breakpoint hit at PC=0x{0:x6} *", mesaPC));
                    stopExecution = true;
                }
            }

            return stopExecution;
        }

        private void ErrorCallback(Exception e)
        {
            // TODO: be more helpful.
            BeginInvoke(new StatusDelegate(RefreshPostExecution), String.Format("* Execution Error {0} *", e.Message));
        }


        /// <summary>
        /// Invoked on the UI thread
        /// </summary>
        /// <param name="message"></param>
        private void RefreshPostExecution(string message)
        {
            PrintIOPStatus();
            WriteLine(String.Empty);
            PrintCPStatus();
            
            WriteLine(message);
            DisplayCurrentCode();
        }

        private void DumpMemory(ushort address, ushort length, string outFile)
        {
            int byteNum = 0;
            StringBuilder line = new StringBuilder();
            line.AppendFormat("{0:x4}: ", address);

            FileStream fs = null;

            if (outFile != null)
            {
                fs = new FileStream(outFile, FileMode.Create, FileAccess.Write);
            }

            for (ushort i = address; i < address + length; i++)
            {
                byte val = _system.IOP.Memory.ReadByte(i);

                line.AppendFormat("{0:x2} ", val);

                byteNum++;
                if ((byteNum % 16) == 0)
                {
                    WriteLine(line.ToString());
                    line.Clear();
                    line.AppendFormat("{0:x4}: ", i + 1);
                    byteNum = 0;
                }

                if (fs != null)
                {
                    fs.WriteByte(val);
                }
            }

            if (byteNum > 0)
            {
                WriteLine(line.ToString());
            }

            if (fs != null)
            {
                fs.Close();
            }
        }

        private void DumpCPMemory(uint address, uint length, string outFile)
        {
            int byteNum = 0;
            StringBuilder line = new StringBuilder();
            line.AppendFormat("{0:x5}: ", address);

            FileStream fs = null;

            if (outFile != null)
            {
                fs = new FileStream(outFile, FileMode.Create, FileAccess.Write);
            }

            for (uint i = address; i < address + length; i++)
            {
                bool valid = false;
                ushort val = _system.MemoryController.DebugMemory.ReadWord((int)i, out valid);

                line.AppendFormat("{0:x4} ", val);

                byteNum++;
                if ((byteNum % 16) == 0)
                {
                    WriteLine(line.ToString());
                    line.Clear();
                    line.AppendFormat("{0:x5}: ", i + 1);
                    byteNum = 0;
                }

                if (fs != null)
                {
                    // fs.WriteByte(val);
                }
            }

            if (byteNum > 0)
            {
                WriteLine(line.ToString());
            }

            if (fs != null)
            {
                fs.Close();
            }
        }

        private void DumpFloppySector(int cylinder, int head, int sector, string outFile)
        {
            int byteNum = 0;
            StringBuilder line = new StringBuilder();
            line.AppendFormat("{0:x4}: ", 0);

            FloppyDisk disk = _system.IOP.FloppyController.Drive.Disk;

            if (cylinder < 0 || cylinder > 76)
            {
                WriteLine("Invalid cylinder spec.");
                return;
            }

            if (head < 0 || head > 1)
            {
                WriteLine("Invalid head spec.");
                return;
            }

            if (disk == null)
            {
                WriteLine("No disk loaded.");
            }
            else
            {
                Sector sectorData = disk.GetSector(cylinder, head, sector - 1);
                WriteLine(String.Format("Sector format {0}, length {1}", sectorData.Format, sectorData.Data.Length));

                FileStream fs = null;

                if (outFile != null)
                {
                    fs = new FileStream(outFile, FileMode.Create, FileAccess.Write);
                }

                for (int i = 0; i < sectorData.Data.Length; i++)
                {
                    byte val = sectorData.Data[i];

                    line.AppendFormat("{0:x2} ", val);

                    byteNum++;
                    if ((byteNum % 16) == 0)
                    {
                        WriteLine(line.ToString());
                        line.Clear();
                        line.AppendFormat("{0:x4}: ", i + 1);
                        byteNum = 0;
                    }

                    if (fs != null)
                    {
                        fs.WriteByte(val);
                    }
                }

                if (byteNum > 0)
                {
                    WriteLine(line.ToString());
                }

                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        private void DumpURegister(int regNum)
        {
            if (regNum < 0 || regNum > 255)
            {
                WriteLine("Invalid U regster.");
                return;
            }

            WriteLine(String.Format("U{0:x2}=0x{1:x4}", regNum, _system.CP.U[regNum]));
        }

        private void DumpMapEntry(int address)
        {
            if (address < 0x10000 || address > 0x13fff)
            {
                WriteLine("Invalid address.");
                return;
            }

            bool valid = false;
            ushort entryWord = _system.MemoryController.DebugMemory.ReadWord(address, out valid);

            MapEntry entry = new MapEntry(entryWord);

            WriteLine(String.Format("Map entry at 0x{0:x5} - {1}", address, entry.ToString()));
        }

        private void TranslateVirtualAddress(int vAddress)
        {
            if (vAddress < 0 || vAddress > 0x1000000)
            {
                WriteLine("Invalid address.");
                return;
            }

            int mapAddr = 0x10000 + (vAddress >> 8);
            int pageOffset = vAddress & 0xff;

            bool valid = false;
            ushort entryWord = _system.MemoryController.DebugMemory.ReadWord(mapAddr, out valid);

            MapEntry entry = new MapEntry(entryWord);

            WriteLine(String.Format("Map entry at 0x{0:x5} - {1}", mapAddr, entry.ToString()));
            WriteLine(String.Format("VA 0x{0:x6} maps to PA 0x{1:x5}", vAddress, pageOffset + (entry.PageNumber << 8)));
        }

        private void TranslatePhysicalAddress(int pAddress)
        {
            if (pAddress < 0 || pAddress > _system.MemoryController.DebugMemory.Size - 1)
            {
                WriteLine("Invalid address.");
                return;
            }

            int found = 0;

            for (int mapAddr = 0x10000; mapAddr < 0x14000; mapAddr++)
            {
                bool valid = false;
                ushort entryWord = _system.MemoryController.DebugMemory.ReadWord(mapAddr, out valid);

                MapEntry entry = new MapEntry(entryWord);

                if ((pAddress >> 8) == entry.PageNumber)
                {
                    int vAddress = ((mapAddr & 0xffff) << 8) | (pAddress & 0xff);
                    WriteLine(String.Format("Map entry at 0x{0:x5} - {1}", mapAddr, entry.ToString()));
                    WriteLine(String.Format("PA 0x{0:x5} maps to VA 0x{1:x6}", pAddress, vAddress));
                    found++;
                }
            }

            if (found == 0)
            {
                WriteLine("No map entry for physical address.");
            }
        }

        private struct MapEntry
        {
            public MapEntry(ushort entryWord)
            {
                PageNumber = ((entryWord & 0xf) << 8) | (entryWord >> 8);
                DP = (entryWord & 0x80) != 0;
                W = (entryWord & 0x40) != 0;
                D = (entryWord & 0x20) != 0;
                RP = (entryWord & 0x10) != 0;
            }

            public int PageNumber;
            public bool DP;     // Dirty & Present
            public bool W;      // Write Protected
            public bool D;      // Dirty
            public bool RP;     // Referenced & Present

            public override string ToString()
            {
                return String.Format("Page 0x{0:x4} {1}{2}{3}{4}",
                    PageNumber,
                    DP ? "dp " : String.Empty,
                    W ? "w " : String.Empty,
                    D ? "d " : String.Empty,
                    RP ? "rp " : String.Empty);
            }
        }

        private void SetMesaBreakpoint(int address)
        {
            BreakpointManager.SetBreakpoint(new BreakpointEntry(BreakpointProcessor.Mesa, BreakpointType.Execution, address));
        }

        private void ClearMesaBreakpoint(int address)
        {
            BreakpointManager.SetBreakpoint(new BreakpointEntry(BreakpointProcessor.Mesa, BreakpointType.None, address));
        }

        private void AnalyzeDisk()
        {
            SA1000Drive drive = _system.HardDrive;

            WriteLine(String.Format("Drive is type {0}", drive.Type));

            for (int cylinder = 0; cylinder < drive.Geometry.Cylinders; cylinder++)
            {                
                for (int head = 0; head < drive.Geometry.Heads; head++)
                {
                    int sector = -1;
                    int field = 0;

                    for (int word = 0; word < drive.WordsPerTrack; word++)
                    {
                        //
                        // Walk the track, looking for sector marks.
                        // There should be 16 sets of:
                        //  - Header mark
                        //  - Label mark
                        //  - Data mark
                        //  - CRC mark
                        // In exactly that order
                        //
                        // Verify the header data
                        //
                        uint data = drive.DebugRead(cylinder, head, word);
                        bool isAddressMark = (data & 0x10000) != 0;
                        bool isCRC = (data & 0x20000) != 0;

                        switch (field)
                        {
                            case 0:
                                if (data == 0x1a141)    // header
                                {
                                    // This is the header mark, increment the sector count
                                    // move to next field.
                                    sector++;
                                    field++;
                                }
                                else if (data == 0x1a143)   // label / data
                                {
                                    // Unexpected here.
                                    WriteLine(String.Format("Unexpected label/data mark before header mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word));
                                }
                                else if (data == 0x2beef)
                                {
                                    /*
                                    // Unexpected here.
                                    WriteLine(String.Format("Unexpected CRC before header mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word)); */
                                }
                                break;

                            case 1:
                                if (data == 0x1a143)    // label / data
                                {
                                    // This is the label mark, move to next field.
                                    field++;
                                }
                                else if (data == 0x1a141)   // header
                                {
                                    // Unexpected header mark here.
                                    WriteLine(String.Format("Unexpected header mark after header mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word));
                                }
                                else if (data == 0x2beef)
                                {
                                    /*
                                    // Unexpected here.
                                    WriteLine(String.Format("Unexpected CRC after header mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word)); */
                                }
                                break;

                            case 2:
                                if (data == 0x1a143)    // label / data
                                {
                                    // This is the data mark, move to CRC.
                                    field++;
                                }
                                else if (data == 0x1a141)   // header
                                {
                                    // Unexpected header mark here.
                                    WriteLine(String.Format("Unexpected header mark after label mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word));
                                }
                                else if (data == 0x2beef)
                                {
                                    /*
                                    // Unexpected here.
                                    WriteLine(String.Format("Unexpected CRC after label mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word)); */
                                }
                                break;

                            case 3:
                                if (data == 0x2beef)    // CRC
                                {
                                    // This is the CRC, move back to header state.
                                    field = 0;
                                }
                                else if (isAddressMark)
                                {
                                    // Unexpected header mark here.
                                    WriteLine(String.Format("Unexpected address mark after data mark at c/h/s (w) {0}/{1}/{2} ({3})",
                                        cylinder,
                                        head,
                                        sector,
                                        word));
                                }
                                break;
                        }
                    }

                    // Check sector count.  Should be 15.
                    if (sector != 15)
                    {
                        WriteLine(String.Format("Unexpected sector count {0} at c/h {1}/{2}",
                            sector,
                            cylinder,
                            head));
                    }

                }
            }
        }


        private void DumpTrack(int cylinder, int head)
        {
            SA1000Drive drive = _system.HardDrive;

            int sector = 0;
            int wordIndex = 0;
            uint data = 0;

            List<uint> sectorData = new List<uint>();

            // Print lead-in to first sector
            while (wordIndex < drive.WordsPerTrack)
            {
                data = drive.DebugRead(cylinder, head, wordIndex);
                wordIndex++;

                if (data == 0x1a141)
                {
                    break;
                }
                else
                {
                    sectorData.Add(data);
                }                
            }

            WriteLine("Lead-in to sector 0:");
            PrintSectorData(sectorData);

            sectorData.Clear();

            // Print sector data            
            while (wordIndex < drive.WordsPerTrack)
            {
                data = drive.DebugRead(cylinder, head, wordIndex);
                wordIndex++;

                if (data == 0x1a141 || wordIndex == drive.WordsPerTrack - 1)
                {
                    WriteLine(String.Format("Sector {0}:", sector));
                    PrintSectorData(sectorData);
                    sectorData.Clear();

                    sector++;
                }
                else
                {
                    sectorData.Add(data);
                }

            }
        }

        private void PrintSectorData(List<uint> data)
        {

            int byteNum = 0;
            StringBuilder line = new StringBuilder();
            line.AppendFormat("000: ");

            for (int i = 0; i < data.Count; i++)
            {
                line.AppendFormat("{0:x5} ", data[i]);

                byteNum++;
                if ((byteNum % 16) == 0)
                {
                    WriteLine(line.ToString());
                    line.Clear();
                    line.AppendFormat("{0:x3}: ", i + 1);
                    byteNum = 0;
                }
            }

            if (byteNum > 0)
            {
                WriteLine(line.ToString());
            }
        }


        private void DisplayCurrentCode()
        {
            _iopDebugger.DisplayCurrentCode();
            _cpDebugger.DisplayCurrentCode();
        }

        private void DisplayHelp()
        {
            WriteLine(@"
                ?, help        -   Display this message.
                .               -   Display IOP and CP status.
                i               -   Single step IOP.
                s               -   Single step CP.
                m               -   Single step macrocode.
                g               -   Start/continue system execution.
                tpc             -   Display TPC registers.
                task <type>     -   Select Task to debug (skips over microcode execution
                                for other tasks while single-stepping.)
                id <address> [count] [toFile]   -   Dump IOP memory.
                cd <address> [count] [toFile]   -   Dump CP memory.
                fd <cyl> <head> <sector>        -   Dump floppy sector.
                u <regnum>      -   Display specified U register.
                mapv <vaddr>    -   Map virtual address to physical.
                mapp <paddr>    -   Map physical address to virtual.
                mapd <maddr>    -   Dump map entry at specified map address.
                mbs <addr>      -   Set Macroinstruction breakpoint.
                mbc <addr>      -   Clear Macroinstruction breakpoint.
                dt <cyl> <head> -   Dump hard disk track.
                save            -   Commit source annotations.
                clear           -   Clear debugger scrollback.
                "
                );
        }

        private void WriteLine(string line)
        {
            DebugOutput.Text += line + "\r\n";
            DebugOutput.Select(DebugOutput.TextLength - 1, 1);
            DebugOutput.ScrollToCaret();
        }

        private void Write(string line)
        {
            DebugOutput.Text += line;
            DebugOutput.Select(DebugOutput.TextLength - 1, 1);
            DebugOutput.ScrollToCaret();
        }

        private void PrintIOPStatus()
        {            
            WriteLine(String.Format("IOP PC=${0:x4} SP=${1:x4} AF=${2:x4} BC=${3:x4} DE=${4:x4} HL=${5:x4}",
                _system.IOP.CPU.PC, _system.IOP.CPU.SP, _system.IOP.CPU.AF, _system.IOP.CPU.BC, _system.IOP.CPU.DE, _system.IOP.CPU.HL));
            WriteLine(String.Format("Flags {0}", GetStringForFlags(_system.IOP.CPU.F)));

            SourceEntry entry = _iopDebugger.SourceMap.GetSourceForAddress(_system.IOP.CPU.PC);
            string symbolName = null;
            string currentSymbol = String.Empty;

            if (entry != null)
            {
                if (entry.SymbolNames.Length == 0 ||
                    entry.SymbolNames[0] == "*none*" || // TODO: move to constant
                    entry.Address != _system.IOP.CPU.PC)
                {
                    // No symbol name associated with this entry, find the nearest.
                    SourceEntry symbolEntry = _iopDebugger.SourceMap.GetNearestSymbolForAddress(_system.IOP.CPU.PC);

                    if (symbolEntry != null)
                    {
                        symbolName = String.Format("{0}+${1:x}", symbolEntry.SymbolNames[0], _system.IOP.CPU.PC - symbolEntry.Address);
                    }
                }
                else
                {
                    symbolName = entry.SymbolNames[0];
                }

                if (symbolName != null)
                {
                    currentSymbol = String.Format("{0},{1} line {2}", symbolName, entry.SourcePath, entry.LineNumber);
                }
            }

            WriteLine(String.Format("${0:x4} ({1})\r\n  {2}", _system.IOP.CPU.PC, currentSymbol, _system.IOP.CPU.Disassemble(_system.IOP.CPU.PC)));

            // Update the IOP debugger's title bar with current MP value
            _iopDebugger.Text = String.Format("IOP Debugger - MP {0}", _system.IOP.MiscIO.MPanelBlank ? "<blank>" : _system.IOP.MiscIO.MPanelValue.ToString());         
        }

        private void PrintCPStatus()
        {
            int tpc = _system.CP.TPC[(int)_system.CP.CurrentTask];

            SourceEntry symbolEntry = _cpDebugger.GetSymbolForAddress(tpc);

            string currentSymbol = String.Empty;
            if (symbolEntry != null)
            {
                string symbolName = symbolEntry.SymbolNames.Length > 0 &&
                                    symbolEntry.SymbolNames[0] != "*none*" ? symbolEntry.SymbolNames[0] : String.Empty;

                currentSymbol = String.Format("{0},{1} line {2}", symbolName, symbolEntry.SourcePath, symbolEntry.LineNumber);
            }

            WriteLine(String.Format("CP Task={0} TPC={1:x3} {2}",
                _system.CP.CurrentTask, 
                tpc,
                currentSymbol));            

            StringBuilder regString = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {                
                regString.AppendFormat(" R{0:x}=0x{1:x4} ", i, _system.CP.ALU.R[i]);

                if (((i+1) % 8) == 0)
                {
                    WriteLine(regString.ToString());
                    regString.Clear();
                }
            }

            StringBuilder reghString = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                reghString.AppendFormat(" RH{0:x}=0x{1:x2}  ", i, _system.CP.RH[i]);

                if (((i + 1) % 8) == 0)
                {
                    WriteLine(reghString.ToString());
                    reghString.Clear();
                }
            }

            WriteLine(reghString.ToString());

            StringBuilder stackString = new StringBuilder();
            // By convention, R0 is TOS in Mesa.
            stackString.AppendFormat("0x{0:x4} ", _system.CP.ALU.R[0]);

            for (int i = _system.CP.StackP; i > 0; i--)
            {
                stackString.AppendFormat("{0:x4} ", _system.CP.U[i]);
            }

            WriteLine(String.Format(" stackP=0x{0:x1} stack: {1}", _system.CP.StackP, stackString));

            WriteLine(String.Format(
                " Q=0x{0:x4} MAR=0x{1:x5} MD=0x{2:x4} pc16={3} ibPtr={4} ibFront=0x{5:x2} ib[0]=0x{6:x2} ib[1]=0x{7:x2}", 
                _system.CP.ALU.Q, 
                _system.MemoryController.MAR, 
                _system.MemoryController.MD,
                _system.CP.PC16 ? 1 : 0,
                _system.CP.IBPtr, 
                _system.CP.IBFront,
                _system.CP.IB[0],
                _system.CP.IB[1]));

            WriteLine(String.Format(" Z={0} N={1} Nb={2} Pg={3} C={4} O={5}",
                _system.CP.ALU.Zero,
                _system.CP.ALU.Neg,
                _system.CP.ALU.NibCarry,
                _system.CP.ALU.PgCarry,
                _system.CP.ALU.CarryOut,
                _system.CP.ALU.Overflow));

            Microinstruction inst = new Microinstruction(_system.CP.MicrocodeRam[tpc]);
            int nia = inst.INIA | _system.CP.NIAModifier;

            WriteLine(String.Format("{0:x3} {1:x12} - {2} (NIA={3:x3}) [c{4}]",
                tpc,
                _system.CP.MicrocodeRam[tpc],
                inst.Disassemble(_system.CP.Cycle),
                nia,
                _system.CP.Cycle));
        }

        private void PrintMesaStatus()
        {
            int pc = ((((_system.CP.RH[5] & 0xf) << 16) | _system.CP.ALU.R[5]) << 1) | (_system.CP.PC16 ? 1 : 0);

            WriteLine(String.Format("Mesa PC=0x{0:x5} (physical address 0x{1:x5})",
                pc,
                pc >> 1));

            StringBuilder stackString = new StringBuilder();

            // By convention, R0 is TOS in Mesa.
            stackString.AppendFormat("0x{0:x4} ", _system.CP.ALU.R[0]);
            for (int i = _system.CP.StackP; i > 0; i--)
            {
                stackString.AppendFormat("0x{0:x4} ", _system.CP.U[i]);
            }

            WriteLine(String.Format(" stackP=0x{0:x1} stack: {1}", _system.CP.StackP, stackString));

            WriteLine(String.Format(
                " ibPtr={0} ibFront=0x{1:x2} ib[0]=0x{2:x2} ib[1]=0x{3:x2}",
                _system.CP.IBPtr,
                _system.CP.IBFront,
                _system.CP.IB[0],
                _system.CP.IB[1]));

            // Since this breakpoint should always happen the microinstruction after an IBDisp has taken place, 
            // the TPC is always pointing to the dispatch address for the bytecode, which is not coincidentally 
            // the bytecode itself.  (The value of ibFront that caused the dispatch has since been discarded, or
            // we'd use that.)
            byte byteCode = (byte)(_system.CP.TPC[(int)TaskType.Emulator]);
            MacroInstruction mInst = MacroInstruction.GetInstruction(MacroType.Lisp, byteCode);

            string operand = string.Empty;
            switch (mInst.Operand)
            {
                case MacroOperand.None:
                    // No operands.
                    break;

                case MacroOperand.Byte:
                    operand = String.Format("0x{0:x2}", _system.CP.IBFront);
                    break;

                case MacroOperand.SignedByte:
                    operand = String.Format("{0}", (sbyte)_system.CP.IBFront);
                    break;

                case MacroOperand.Pair:
                    operand = String.Format("0x{0:x1},,0x{1:x1}", _system.CP.IBFront >> 4, _system.CP.IBFront & 0xf);
                    break;

                case MacroOperand.TwoByte:
                    operand = String.Format("0x{0:x2},,0x{1:x2}", 
                        _system.CP.IBFront, 
                        _system.CP.IBPtr == IBState.Word ? _system.CP.IB[1] : _system.CP.IB[0]);
                    break;

                case MacroOperand.Word:
                    operand = String.Format("0x{0:x4}",
                        (_system.CP.IBFront << 8) | (_system.CP.IBPtr == IBState.Word ? _system.CP.IB[1] : _system.CP.IB[0]));
                    break;
            }

            WriteLine(String.Format("Bytecode 0x{0:x2} - {1} {2}", byteCode, mInst.Mnemonic, operand));
        }

        private void DisplayTPCRegisters()
        {
            for(int i=0;i<8;i++)
            {
                WriteLine(String.Format("{0} - 0x{1:x3}", (TaskType)i, _system.CP.TPC[i]));
            }
        }

        private void InjectKeystroke(int keycode)
        {
            _system.IOP.Keyboard.KeyDown((KeyCode)keycode);
            _system.IOP.Keyboard.KeyUp((KeyCode)keycode);
        }

        private string GetStringForFlags(byte f)
        {
            string flags = String.Empty;

            if (f == 0)
            {
                flags = "NONE";
                return flags;
            }

            if ((f & 0x80) != 0)
            {
                flags += "S ";
            }

            if ((f & 0x40) != 0)
            {
                flags += "Z ";
            }

            if ((f & 0x10) != 0)
            {
                flags += "AC ";
            }

            if ((f & 0x04) != 0)
            {
                flags += "P ";
            }

            if ((f & 0x01) != 0)
            {
                flags += "CY ";
            }

            return flags;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _iopDebugger.Close();
            _cpDebugger.Close();
        }

        private DSystem _system;

        private string _lastCommand;
        private bool _singleStepIOP;
        private bool _singleStepCP;
        private bool _singleStepMesa;
        private TaskType _debugTask;
        private bool _debugSpecificTask;
        private int _stepCount;
        private DebuggerReason _reason;
        private string _entryMessage;

        private IOPDebugger _iopDebugger;
        private CPDebugger _cpDebugger;

        private delegate void StatusDelegate(string message);

        
    }
}
