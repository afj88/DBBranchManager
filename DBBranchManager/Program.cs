using System;
using System.Threading;
using DBBranchManager.Config;
using DBBranchManager.Utils;

namespace DBBranchManager
{
    public static class Program
    {
        private static readonly TimeSpan FastExceptionTriggerSpan = TimeSpan.FromSeconds(1);
        private const int FastExceptionBreakThreshold = 3;

        private static readonly SingleThreadSynchronizationContext SyncContext = new SingleThreadSynchronizationContext();
        private const string ConfigFilePath = @"..\..\config.json";

        public static void Main(string[] args)
        {
            var restart = false;
            var exceptionsCount = 0;
            var lastExceptionTime = DateTime.UtcNow;

            Application app = null;
            var watcher = new EnhanchedFileSystemWatcher();
            watcher.Changed += (sender, eventArgs) =>
            {
                Post(() =>
                {
                    Console.WriteLine("\nChanges detected in config file. Restarting...");
                    restart = true;
                    StartApp(ref app);
                });
            };

            watcher.AddWatch(ConfigFilePath, null);

            do
            {
                restart = false;
                try
                {
                    Run(() => StartApp(ref app));
                }
                catch (SynchronizationContextCompletedException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    var sinceLastException = DateTime.UtcNow - lastExceptionTime;
                    if (sinceLastException < FastExceptionTriggerSpan)
                    {
                        if (++exceptionsCount >= FastExceptionBreakThreshold)
                        {
                            Console.WriteLine(@"
Too many exception happened in too short a time. This probably means you have a broken config.
Try to fix the problem, then press any key to continue...");
                            Console.ReadKey(true);
                            exceptionsCount = 0;
                        }
                    }
                    else
                    {
                        exceptionsCount = 0;
                    }
                    lastExceptionTime = DateTime.UtcNow;
                    Console.WriteLine("\nRestarting...");
                    restart = true;
                }
            } while (restart);
        }

        private static void StartApp(ref Application curApp)
        {
            if (curApp != null)
                curApp.Dispose();

            var config = Configuration.LoadFromJson(ConfigFilePath);
            curApp = new Application(config);
            curApp.Start();
        }

        private static void Run(Action func)
        {
            SynchronizationContext.SetSynchronizationContext(SyncContext);

            Post(func);

            SyncContext.Run();
        }

        public static void Post(Action func)
        {
            SyncContext.Post(state => func(), null);
        }

        public static void Exit()
        {
            SyncContext.Complete();
        }
    }
}