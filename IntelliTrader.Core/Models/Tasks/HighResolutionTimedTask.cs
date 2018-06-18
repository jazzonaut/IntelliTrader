using System;
using System.Diagnostics;
using System.Threading;

namespace IntelliTrader.Core
{
    public abstract class HighResolutionTimedTask : ITimedTask
    {
        /// <summary>
        /// Raised on unhandled exception
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledException;

        /// <summary>
        /// Delay before starting the task in milliseconds
        /// </summary>
        public double StartDelay { get; set; } = 0;

        /// <summary>
        /// Periodic execution interval in milliseconds
        /// </summary>
        public double Interval { get; set; } = 1000;

        /// <summary>
        /// The priority of the timer thread
        /// </summary>
        public ThreadPriority Priorty { get; set; } = ThreadPriority.Normal;

        /// <summary>
        /// Stopwatch to use for timing the intervals
        /// </summary>
        public Stopwatch Stopwatch { get; set; }

        /// <summary>
        /// Indicates whether the task is currently running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Number of times the task has been run
        /// </summary>
        public long RunCount { get; private set; }

        /// <summary>
        /// Total time it took to run the task in milliseconds
        /// </summary>
        public double TotalRunTime { get; private set; }

        /// <summary>
        /// Total task run delay in milliseconds
        /// </summary>
        public double TotalLagTime { get; private set; }

        private Thread timerThread;
        private Stopwatch runWatch;
        private ManualResetEvent resetEvent;

        /// <summary>
        /// Starts the task
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                runWatch = new Stopwatch();
                resetEvent = new ManualResetEvent(false);

                timerThread = new Thread(() =>
                {
                    if (Stopwatch == null)
                    {
                        Stopwatch = Stopwatch.StartNew();
                    }
                    else if (!Stopwatch.IsRunning)
                    {
                        Stopwatch.Restart();
                    }

                    long startTime = Stopwatch.ElapsedMilliseconds;
                    double nextRunTime = StartDelay + Interval;

                    while (IsRunning)
                    {
                        long elapsedTime = Stopwatch.ElapsedMilliseconds;
                        double waitTime = nextRunTime - (elapsedTime - startTime);
                        if (waitTime > 0)
                        {
                            if (resetEvent.WaitOne((int)(waitTime)))
                            {
                                break;
                            }
                        }

                        runWatch.Restart();
                        try
                        {
                            Run();
                        }
                        catch (Exception ex)
                        {
                            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                        }

                        long runTime = runWatch.ElapsedMilliseconds;
                        TotalLagTime += runTime - Interval;
                        TotalRunTime += runTime;
                        RunCount++;
                        nextRunTime += Interval;
                    }
                });

                timerThread.Priority = Priorty;
                timerThread.Start();
            }
        }

        /// <summary>
        /// Stops the task
        /// </summary>
        public void Stop()
        {
            Stop(true);
        }

        /// <summary>
        /// Stops the task
        /// </summary>
        /// <remarks>
        /// This function is waiting an executing thread (unless join is set to false).
        /// </remarks>
        public void Stop(bool terminateThread)
        {
            if (IsRunning)
            {
                IsRunning = false;
                resetEvent.Set();
                runWatch.Stop();

                if (!terminateThread)
                {
                    timerThread?.Join();
                    timerThread = null;
                }

                resetEvent.Dispose();
            }
        }

        public abstract void Run();
    }
}
