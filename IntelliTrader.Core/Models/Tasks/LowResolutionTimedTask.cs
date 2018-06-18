using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace IntelliTrader.Core
{
    public abstract class LowResolutionTimedTask : ITimedTask
    {
        /// <summary>
        /// Delay before starting the task in milliseconds
        /// </summary>
        public double StartDelay { get; set; }

        /// <summary>
        /// Periodic execution interval in milliseconds
        /// </summary>
        public double Interval
        {
            get { return timer.Interval; }
            set { timer.Interval = value; }
        }

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

        private readonly Timer timer = new Timer();
        private readonly Stopwatch runWatch = new Stopwatch();
        private readonly System.Threading.AutoResetEvent syncMutex = new System.Threading.AutoResetEvent(true);

        /// <summary>
        /// Class constructor. It allocates the memory for the background timer and
        /// initializes sync mutex.
        /// </summary>
        public LowResolutionTimedTask()
        {
            this.timer.Elapsed += OnTimerElapsed;
        }

        /// <summary>
        /// Starts the background task timer that is in charge of executing the Execute method each
        /// time the interval is elapsed.
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                if (StartDelay > 0)
                {
                    Task.Delay((int)StartDelay).ContinueWith(t =>
                    {
                        if (IsRunning)
                        {
                            timer.Start();
                        }
                    });
                }
                else
                {
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// Stops the background task timer that is in charge of executing the Execute method each
        /// time the interval is elapsed. If the Execute method was executing when this method is
        /// called, the caller thread will block waiting the Execute operation to finish. Later on,
        /// the timer will be stopped. Otherwise, if the Execute method is not executing when this
        /// method is called, the timer will be stopped without blocking the caller thread.
        /// </summary>
        public void Stop()
        {
            Stop(-1);
        }

        /// <summary>
        /// Stops the background task timer that is in charge of executing the Execute method each
        /// time the interval is elapsed. If the Execute method was executing when this method is
        /// called, the caller thread will block waiting the Execute operation to finish. Later on,
        /// the timer will be stopped. Otherwise, if the Execute method is not executing when this
        /// method is called, the timer will be stopped without blocking the caller thread.
        /// </summary>
        /// <param name="timeout">Timeout value in milliseconds to wait before killing the task</param>
        public void Stop(int timeout)
        {
            if (IsRunning)
            {
                this.syncMutex.WaitOne(timeout);
                this.syncMutex.Set();
                this.timer.Stop();
                IsRunning = false;
            }
        }

        /// <summary>
        /// Stops the periodic task executor without waiting the current task to stop.
        /// </summary>
        public void Terminate()
        {
            if (IsRunning)
            {
                this.timer.Stop();
                IsRunning = false;
            }
        }

        /// <summary>
        /// This method can operate in two different ways. If the Execute method is currently executing, it will
        /// block the caller thread until Execute finishes. However, if the Execute method is not being executed,
        /// this method will not block and will immediately return back the control to the caller thread.
        /// </summary>
        public void Join()
        {
            if (IsRunning)
            {
                this.syncMutex.WaitOne();
                this.syncMutex.Set();
            }
        }

        /// <summary>
        /// Wraps the Execute call abstracting the child class from the thread synchronization issues.
        /// </summary>
        /// <param name="sender">The thimer object that is calling the event listener.</param>
        /// <param name="e">The arguments passed by the timer to the method.</param>
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //Force other threads to wait until it's finished when calling join.
            this.syncMutex.Reset();

            //Avoid re-calling the method while it is still operating.
            this.timer.Stop();

            if (IsRunning)
            {
                runWatch.Restart();
                this.Run();
                long runTime = runWatch.ElapsedMilliseconds;
                TotalLagTime += (runTime > Interval) ? (runTime - Interval) : 0;
                TotalRunTime += runTime;
                RunCount++;
                runWatch.Stop();

                //Re-Start the timer to execute the worker function endlessly.
                this.timer.Start();
            }

            //Release threads that might be frozen in join operation.
            this.syncMutex.Set();
        }

        /// <summary>
        /// This method must be implemented by the child class and must contain the code
        /// to be executed periodically.
        /// </summary>
        public abstract void Run();
    }
}

