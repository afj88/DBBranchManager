using System.Collections.Generic;
using System.Linq;
using DBBranchManager.Config;
using DBBranchManager.Dependencies;

namespace DBBranchManager.Components
{
    internal class DeployComponent : AggregatorComponent
    {
        private readonly IDependencyGraph<BranchInfo> mBranchGraph;
        private readonly BranchInfo mActiveBranch;
        private readonly BranchInfo mBackupBranch;
        private readonly DatabaseInfo[] mDatabasesInfos;

        public DeployComponent(IDependencyGraph<BranchInfo> branchGraph, string backupBranch, string activeBranch, IEnumerable<DatabaseInfo> databasesInfos) :
            base(null, "Done!")
        {
            var branchesByName = branchGraph.GetPath().ToDictionary(x => x.Name);
            mBranchGraph = branchGraph;
            mBackupBranch = branchesByName[backupBranch];
            mActiveBranch = branchesByName[activeBranch];
            mDatabasesInfos = databasesInfos.ToArray();
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            if (runContext.SkipRestore)
            {
                foreach (var databaseInfo in mDatabasesInfos)
                {
                    yield return new LogComponent(string.Format("Restore {0}: Skipped", databaseInfo.Name));
                }
            }
            else
            {
                foreach (var databaseInfo in mDatabasesInfos)
                {
                    yield return new RestoreDatabaseComponent(databaseInfo);
                }
            }

            foreach (var branchInfo in mBranchGraph.GetPath(mBackupBranch, mActiveBranch))
            {
                yield return new BranchComponent(branchInfo, mDatabasesInfos[0].Connection);
            }
        }

        protected override IComponent GetPreComponent(string action, ComponentRunContext runContext)
        {
            return new LogComponent(string.Format("Beginning deploy: {0}", runContext.Environment));
        }
    }
}