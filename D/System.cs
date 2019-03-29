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
using D.Display;
using D.Ethernet;
using D.IO;
using D.IOP;
using D.Memory;
using D.UI;
using System;
using System.Threading;

namespace D
{

    public delegate bool StepCallbackDelegate();

    public delegate void ErrorCallbackDelegate(Exception e);

    /// <summary>
    /// Defines the context for the current execution
    /// (debug hooks, etc)
    /// </summary>
    public class SystemExecutionContext
    {
        public SystemExecutionContext(StepCallbackDelegate step8085, StepCallbackDelegate stepCP, StepCallbackDelegate stepMesa, ErrorCallbackDelegate error)
        {
            StepCallback8085 = step8085;
            StepCallbackCP = stepCP;
            StepCallbackMesa = stepMesa;
            ErrorCallback = error;
        }

        public readonly StepCallbackDelegate StepCallback8085;
        public readonly StepCallbackDelegate StepCallbackCP;
        public readonly StepCallbackDelegate StepCallbackMesa;
        public readonly ErrorCallbackDelegate ErrorCallback;
    }


    /// <summary>
    /// Encompasses the Star's hardware and provides functionality to run and debug the system.
    /// </summary>
    public class DSystem
    {
        public DSystem()
        {
            _scheduler = new Scheduler();

            _cp = new CentralProcessor(this);
            _iop = new IOProcessor(this);
            _memoryController = new MemoryController();
            _displayController = new DisplayController(this);
            _hardDrive = new SA1000Drive(this);
            _shugartController = new ShugartController(this, _hardDrive);
            _ethernetController = new EthernetController(this);

            try
            {
                _frameTimer = new FrameTimer(38.7);
            }
            catch
            {
                // Not supported on this platform.
                _frameTimer = null;
            }
        }

        public void Reset()
        { 
            bool wasExecuting = IsExecuting;

            // Save context and stop executing if the system is currently running.
            SystemExecutionContext context = null;
            if (wasExecuting)
            {
                context = _currentExecutionContext;
                StopExecution();
            }

            // Now do the actual reset.
            _cp.Reset();
            _iop.Reset();
            _displayController.Reset();
            _ethernetController.Reset();
            _hardDrive.Reset();
            _shugartController.Reset();
            // _scheduler.Reset();

            _cpCycles = 0;
            _elapsedCycles = 0;

            // Restart execution if we were running before the reset.
            if (wasExecuting)
            {
                StartExecution(context);
            }
        }

        public void Shutdown(bool commitDisks)
        {
            Console.WriteLine("Saving disk images and shutting down.  Please wait...");
            _hardDrive.Save();
            _ethernetController.Shutdown();
        }

        public bool IsExecuting
        {
            get { return _executionThread != null && _executionThread.IsAlive; }
        }

        public SystemExecutionContext ExecutionContext
        {
            get { return _currentExecutionContext; }
        }
            

        public Scheduler Scheduler
        {
            get { return _scheduler; }
        }

        public IOProcessor IOP
        {
            get { return _iop; }
        }

        public CentralProcessor CP
        {
            get { return _cp; }
        }

        public MemoryController MemoryController
        {
            get { return _memoryController; }
        }

        public DisplayController DisplayController
        {
            get { return _displayController; }
        }

        public ShugartController ShugartController
        {
            get { return _shugartController; }
        }

        public SA1000Drive HardDrive
        {
            get { return _hardDrive; }
        }

        public EthernetController EthernetController
        {
            get { return _ethernetController; }
        }

        public DWindow Display
        {
            get { return _display; }
        }

        /// <summary>
        /// Allows UI to be alerted when execution state changes
        /// </summary>
        public delegate void ExecutionStateChangedDelegate();

        public ExecutionStateChangedDelegate ExecutionStateChanged;

        public void AttachDisplay(DWindow display)
        {
            _display = display;
        }

        public void StartExecution(SystemExecutionContext context)
        {
            StopExecution();
            
            if (_executionThread == null || !_executionThread.IsAlive)
            {
                if (context.StepCallback8085 != null &&
                    context.StepCallbackCP != null &&
                    context.StepCallbackMesa != null)
                {
                    _executionThread = new Thread(new ParameterizedThreadStart(DebugExecutionWorker));
                }
                else
                {
                    _executionThread = new Thread(new ParameterizedThreadStart(ExecutionWorker));
                }
                _executionThread.Start(context);

                ExecutionStateChanged();

                _currentExecutionContext = context;
            }
        }

        public void StopExecution()
        {
            if (_executionThread != null && _executionThread.IsAlive)
            {
                _abortExecution = true;
                _executionThread.Join();

                _executionThread = null;
                _currentExecutionContext = null;

                ExecutionStateChanged();
            }
        }

        private void DebugExecutionWorker(object obj)
        {
            SystemExecutionContext context = (SystemExecutionContext)obj;

            _abortExecution = false;
            bool iopAbort = false;

            while (!_abortExecution)
            {
                try
                {
                    //
                    // We clock the IOP first and let it run an instruction.
                    // This returns the number of clock cycles consumed --
                    // on the 8085's 3Mhz timebase.
                    // 
                    // We then execute the corresponding number of CP cycles
                    // (based on a 137ns clock period, or about 7.3Mhz) that would
                    // occur during that period.
                    //
                    if (_cpCycles == 0)
                    {
                        if (iopAbort)
                        {
                            //
                            // Out of cycles after an IOP abort, now we actually abort.
                            //
                            _abortExecution = true;
                        }
                        else
                        {
                            int i8085Cycles = _iop.Execute();

                            // This is inexact and that's probably good enough on average.
                            _cpCycles = (int)_cpCyclesPer8085Cycle * i8085Cycles;

                            if (context.StepCallback8085())
                            {
                                //
                                // Set our local iopAbort flag, we still want to continue
                                // to execute the CP and scheduler for as many microcycles as correspond to the
                                // above 8085 instruction's execution time to keep things in sync.
                                //
                                iopAbort = true;
                            }
                        }
                    }
                    
                    _cp.ExecuteInstruction(1);
                    _cpCycles--;
                    _elapsedCycles++;
                   
                    if (context.StepCallbackCP())
                    {
                        //
                        // Break on microinstruction step
                        //
                        _abortExecution = true;
                    }
                    
                    if (_cp.IBDispatch && 
                        context.StepCallbackMesa())
                    {
                        //
                        // Break on macroinstruction step
                        //
                        _abortExecution = true;
                    }
                }
                catch(Exception e)
                {
                    context.ErrorCallback(e);
                    _abortExecution = true;
                }
            }

            _cpCycles = 0;

            ExecutionStateChanged();
        }

        private void ExecutionWorker(object obj)
        {
            SystemExecutionContext context = (SystemExecutionContext)obj;

            _abortExecution = false;

            while (!_abortExecution)
            {
                try
                {
                    //
                    // We clock the IOP first and let it run an instruction.
                    // This returns the number of clock cycles consumed --
                    // on the 8085's 3Mhz timebase.
                    // 
                    // We then execute the corresponding number of CP cycles
                    // (based on a 137ns clock period, or about 7.3Mhz) that would
                    // occur during that period.
                    //
                    int i8085Cycles = _iop.Execute();

                    // This is inexact and that's probably good enough on average.
                    _cpCycles = (int)_cpCyclesPer8085Cycle * i8085Cycles;

                    _cp.ExecuteInstruction(_cpCycles);

                    if (Configuration.ThrottleSpeed)
                    {
                        _elapsedCycles += _cpCycles;

                        if (_elapsedCycles > _cpCyclesPerField)
                        {
                            _elapsedCycles -= _cpCyclesPerField;

                            if (_frameTimer != null)
                            {
                                _frameTimer.WaitForFrame();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    context.ErrorCallback(e);
                    _abortExecution = true;
                }
            }

            ExecutionStateChanged();
        }

        //
        // Devices belonging to this system
        //
        private IOProcessor _iop;
        private CentralProcessor _cp;
        private MemoryController _memoryController;
        private DisplayController _displayController;
        private ShugartController _shugartController;
        private EthernetController _ethernetController;
        private SA1000Drive _hardDrive;

        //
        // Display for rendering
        //
        private DWindow _display;

        //
        // System scheduler
        private Scheduler _scheduler;

        //
        // Execution state
        //
        private Thread _executionThread;
        private bool _abortExecution;
        private int _cpCycles;
        private double _elapsedCycles;
        private SystemExecutionContext _currentExecutionContext;

        //
        // Constants
        //

        // Ratio of CP cycles per 8085 cycle (appx. 7.3Mhz to 3.0Mhz)
        private const double _cpCyclesPer8085Cycle = 2.43 * 2.0;

        // Cycles per display field
        private const double _cpCyclesPerField = 7299270.1 / 38.7;

        private FrameTimer _frameTimer;
    }
}
