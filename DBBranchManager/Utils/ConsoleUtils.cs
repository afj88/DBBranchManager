using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBBranchManager.Utils
{
    internal static class ConsoleUtils
    {
        private static readonly Thread ReadThread;
        private static readonly AutoResetEvent GetInputEvent;
        private static readonly AutoResetEvent GotInputEvent;
        private static readonly AutoResetEvent GotCancelEvent;
        private static string Input;

        static ConsoleUtils()
        {
            GetInputEvent = new AutoResetEvent(false);
            GotInputEvent = new AutoResetEvent(false);
            GotCancelEvent = new AutoResetEvent(false);
            ReadThread = new Thread(Read)
            {
                IsBackground = true,
                Name = "ReadLineAsync Thread"
            };
            ReadThread.Start();
        }

        private static void Read()
        {
            while (true)
            {
                GetInputEvent.WaitOne();
                Input = Console.ReadLine();
                GotInputEvent.Set();
            }
        }

        public static Task<string> ReadLineAsync(CancellationToken ct)
        {
            GetInputEvent.Set();
            ct.Register(() => GotCancelEvent.Set());
            return Task.Run(() =>
            {
                while (true)
                {
                    switch (WaitHandle.WaitAny(new WaitHandle[] { GotInputEvent, GotCancelEvent }))
                    {
                        case 0:
                            return Input;
                        default:
                            ct.ThrowIfCancellationRequested();
                            break;
                    }
                }
            }, ct);
        }
    }
}