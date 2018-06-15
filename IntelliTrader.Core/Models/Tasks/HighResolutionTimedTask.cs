using System;
using System.Diagnostics;
using System.Threading;

namespace IntelliTrader.Core
{
    public abstract class HighResolutionTimedTask
    {
        /// <summary>
        /// Raised on unhandled exception
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledException;
        
        /// <summary>
        /// Delay before starting the task in [ms]
        /// </summary>
        public double StartDelay { get; set; } = 0;

        /// <summary>
        /// The interval of a task in [ms]
        /// </summary>
        public float RunInterval { get; set; } = 1000;

        /// <summary>
        /// The priority of the timer thread
        /// </summary>
        public ThreadPriority Priorty { get; set; } = ThreadPriority.Normal;

        /// <summary>
        /// Restart the timer periodically to prevent precision problems in [s]
        /// </summary>
        public double TimerCorrectionInterval { get; set; } = 3600;

        /// <summary>
        /// True when task is enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Number of times the task has been run
        /// </summary>
        public long RunTimes { get; private set; }

        /// <summary>
        /// Total amount of time spent on waiting for previous execution to complete
        /// </summary>
        public double TotalWaitTime { get; private set; }

        private Thread timerThread;
        private Stopwatch timeWatcher;
        private ManualResetEvent resetEvent;

        public HighResolutionTimedTask()
        {
            this.resetEvent = new ManualResetEvent(false);
            this.timeWatcher = new Stopwatch();
        }

        /// <summary>
        /// Starts the task
        /// </summary>
        public void Start()
        {
            if (!IsEnabled)
            {
                IsEnabled = true;

                timerThread = new Thread(() =>
                {
                    double nextRun = StartDelay + RunInterval;
                    Stopwatch stopWatch = Stopwatch.StartNew();

                    while (IsEnabled)
                    {
                        var waitTime = nextRun - stopWatch.ElapsedMilliseconds;

                        // Restart the timer periodically to prevent precision problems
                        if (stopWatch.Elapsed.TotalSeconds >= TimerCorrectionInterval)
                        {
                            stopWatch.Restart();
                            nextRun = waitTime;
                        }

                        if (waitTime > 0)
                        {
                            if (resetEvent.WaitOne((int)waitTime))
                            {
                                break;
                            }
                        }

                        timeWatcher.Restart();
                        try
                        {
                            Run();
                        }
                        catch (Exception ex)
                        {
                            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                        }
                        TotalWaitTime += timeWatcher.ElapsedMilliseconds - RunInterval;
                        RunTimes++;

                        nextRun += RunInterval;
                    }
                });

                timerThread.Priority = Priorty;
                timerThread.Start();
            }
        }

        /// <summary>
        /// Stops the task
        /// </summary>
        /// <remarks>
        /// This function is waiting an executing thread (unless join is set to false).
        /// </remarks>
        public void Stop(bool terminateThread = true)
        {
            if (IsEnabled)
            {
                IsEnabled = false;
                resetEvent.Set();
                timeWatcher.Stop();

                if (!terminateThread)
                {
                    timerThread?.Join();
                    timerThread = null;
                }
            }
        }

        public abstract void Run();
    }
}
