using System.Collections.Concurrent;

namespace IntelliTrader.Core
{
    public interface ICoreService : IConfigurableService
    {
        ICoreConfig Config { get; }
        string Version { get; }
        void Start();
        void Stop();
        void Restart();
        void AddTask(string name, HighResolutionTimedTask task);
        void RemoveTask(string name);
        void RemoveAllTasks();
        void StartTask(string name);
        void StartAllTasks();
        void StopTask(string name);
        void StopAllTasks();
        HighResolutionTimedTask GetTask(string name);
        ConcurrentDictionary<string, HighResolutionTimedTask> GetAllTasks();
    }
}
