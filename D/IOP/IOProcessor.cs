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

namespace D.IOP
{
    /// <summary>
    /// Encapsulates the entirety of the IOP hardware.
    /// </summary>
    public class IOProcessor
    {
        public IOProcessor(DSystem system)
        {
            _system = system;
            _io = new IOPIOBus();
            _mem = new IOPMemoryBus(_io);
            _cpu = new i8085(_mem, _io);
            _keyboard = new Keyboard();
            _mouse = new Mouse();            

            //
            // 8" floppy drive used by the IOP
            //
            _floppyDrive = new FloppyDrive(_system);

            //
            // Add devices to the IO bus
            //
            _miscIO = new MiscIO(this);
            _floppyController = new FloppyController(_floppyDrive, _system);
            _dma = new DMAController(this);
            _tty = new Printer();
            _beeper = new Beeper();

            //
            // Register DMA devices with controller
            //
            _dma.RegisterDevice(_floppyController, 0);  // Floppy, DMA Channel 0
            _dma.RegisterDevice(_system.CP, 1);         // CP, DMA Channel 1

            _io.RegisterDevice(_miscIO);
            _io.RegisterDevice(_floppyController);
            _io.RegisterDevice(_dma);
            _io.RegisterDevice(_system.CP);
            _io.RegisterDevice(_tty);

            Reset();
        }

        public void Reset()
        {
            _cpu.Reset();
            _miscIO.Reset();
            _dma.Reset();
            _floppyController.Reset();
        }
        
        /// <summary>
        /// Executes a single 8085 instruction or runs the DMA controller if DMA is in progress,
        /// and returns the number of 3Mhz clock cycles consumed.
        /// </summary>
        /// <returns></returns>
        public int Execute()
        {
            //
            // Run the DMA controller, see if it has anything to do this cycle.
            //
            _dma.Execute();
            
            if (_dma.HRQ)
            { 
                // Yes, it executed a DMA transfer which means the CPU doesn't get to run.
                return 4;   // A DMA cycle takes 4 clocks.
            }
            else
            {
                // Run the CPU for one instruction.
                return _cpu.Execute();
            }
        }

        public i8085 CPU
        {
            get { return _cpu; }
        }

        public I8085MemoryBus Memory
        {
            get { return _mem; }
        }

        public MiscIO MiscIO
        {
            get { return _miscIO; }
        }

        public FloppyController FloppyController
        {
            get { return _floppyController; }
        }

        public DMAController DMAController
        {
            get { return _dma; }
        }

        public Keyboard Keyboard
        {
            get { return _keyboard; }
        }

        public Mouse Mouse
        {
            get { return _mouse; }
        }

        public Printer Printer
        {
            get { return _tty; }
        }

        public Beeper Beeper
        {
            get { return _beeper; }
        }

        private i8085 _cpu;
        private IOPIOBus _io;
        private I8085MemoryBus _mem;

        //
        // Devices on the IOP
        //
        private MiscIO _miscIO;
        private FloppyController _floppyController;
        private DMAController _dma;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Printer _tty;
        private Beeper _beeper;
        private DSystem _system;

        //
        // Devices used by the IOP
        //
        private FloppyDrive _floppyDrive;
    }
}
