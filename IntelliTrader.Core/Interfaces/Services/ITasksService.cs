using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ITasksService
    {
        T AddTask<T>(string name, T task, double interval, double startDelay = 0, bool startTask = true, bool runNow = false) where T : ITimedTask;
        void RemoveTask(string name, bool stopTask = true);
        void StartAllTasks();
        void StopAllTasks();
        void RemoveAllTasks();
        ITimedTask GetTask(string name);
        T GetTask<T>(string name);
        IEnumerable<KeyValuePair<string, ITimedTask>> GetAllTasks();
        void SetUnhandledExceptionHandler(UnhandledExceptionEventHandler handler);
    }
}
