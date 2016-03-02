using System;
using System.Linq;
using System.Threading;
using DBBranchManager.Components;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Dependencies;
using DBBranchManager.Utils;

namespace DBBranchManager
{
    internal class Application : IDisposable
    {
        private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly Configuration mConfiguration;
        private readonly IComponent mRootComponent;
        private bool mDisposed;

        public Application(Configuration config)
        {
            mConfiguration = config;
            mRootComponent = new DeployComponent(CreateBranchGraph(config), config.BackupBranch, config.ActiveBranch, config.Databases);
        }

        public void Start()
        {
            Console.WriteLine("Awaiting commands...");
            BeginConsoleInput();
        }

        private IDependencyGraph<BranchInfo> CreateBranchGraph(Configuration config)
        {
            var branchesByName = config.Branches.ToDictionary(x => x.Name);
            var graph = new DependencyGraph<BranchInfo>();

            foreach (var branchInfo in config.Branches)
            {
                if (branchInfo.Parent != null)
                {
                    var source = branchesByName[branchInfo.Parent];
                    var target = branchesByName[branchInfo.Name];
                    graph.AddDependency(source, target);
                }
                else
                {
                    graph.AddNode(branchesByName[branchInfo.Name]);
                }
            }

            return graph;
        }

        private async void BeginConsoleInput()
        {
            while (!mDisposed)
            {
                try
                {
                    var line = await ConsoleUtils.ReadLineAsync(mCancellationTokenSource.Token);
                    if (mDisposed)
                        return;
                    OnConsoleInput(line);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private void OnConsoleInput(string line)
        {
            var argv = line.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (argv.Length == 0)
                return;

            var cmd = argv[0];
            switch (cmd.ToLower())
            {
                case "force":
                case "f":
                {
                    var env = argv.Length > 1 ? argv[1] : mConfiguration.Environment;
                    RunDeploy(env);
                    break;
                }

                case "quit":
                case "q":
                    Program.Exit();
                    break;

                case ActionConstants.GenerateScripts:
                case "gs":
                case "g":
                {
                    var env = argv.Length > 1 ? argv[1] : mConfiguration.Environment;
                    RunAction(ActionConstants.GenerateScripts, env);
                    break;
                }

                case ActionConstants.MakeReleasePackage:
                case "rp":
                {
                    var env = argv.Length > 1 ? argv[1] : mConfiguration.Environment;
                    RunAction(ActionConstants.MakeReleasePackage, env);
                    break;
                }

                case "t":
                    throw new Exception();
            }
        }

        private void RunDeploy(string environment)
        {
            Program.Post(() =>
            {
                Console.WriteLine("[{0:T}] Shit's going down!\n", DateTime.Now);
                Beep("start");

                var runState = new ComponentRunContext(mConfiguration, environment);
                if (RunComponent(ActionConstants.Deploy, runState))
                    Beep("success");
                else
                    Beep("error");
            });
        }

        private void RunAction(string action, string env)
        {
            Program.Post(() => { RunComponent(action, new ComponentRunContext(mConfiguration, env)); });
        }

        private bool RunComponent(string action, ComponentRunContext runState)
        {
            Console.WriteLine("[{0:T}] Running '{1}' action", DateTime.Now, action);

            foreach (var log in mRootComponent.Run(action, runState))
            {
                Console.WriteLine("[{0:T}] {1}{2}", DateTime.Now, new string(' ', runState.Depth * 2), log);
                if (runState.Error)
                {
                    Console.WriteLine("[{0:T}] Blocking Errors Detected ):", DateTime.Now);
                    return false;
                }
            }

            Console.WriteLine("\n[{0:T}] Success!\n", DateTime.Now);
            return true;
        }

        private void Beep(string reason)
        {
            BeepInfo beep;
            if (mConfiguration.Beeps.TryGetValue(reason, out beep))
            {
                Buzzer.Beep(beep.Frequency, beep.Duration, beep.Times, beep.DutyTime);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed)
                return;

            if (disposing)
            {
                mCancellationTokenSource.Cancel();
                mCancellationTokenSource.Dispose();
            }

            mDisposed = true;
        }
    }
}