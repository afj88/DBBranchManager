using System.Collections.Generic;
using System.IO;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Dependencies;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class ReleaseInfo
    {
        private readonly BranchInfo mBranch;
        private readonly string mPath;
        private readonly string mName;

        public ReleaseInfo(BranchInfo branch, string path)
        {
            mBranch = branch;
            mPath = path;
            mName = System.IO.Path.GetFileName(path);
        }

        public BranchInfo Branch
        {
            get { return mBranch; }
        }

        public string Path
        {
            get { return mPath; }
        }

        public string Name
        {
            get { return mName; }
        }
    }

    internal class ReleaseComponent : AggregatorComponent
    {
        private readonly ReleaseInfo mReleaseInfo;
        private readonly DatabaseConnectionInfo mDbConnection;

        public ReleaseComponent(BranchInfo branchInfo, string releaseDir, DatabaseConnectionInfo dbConnection) :
            base(string.Format("Release '{0}'", Path.GetFileName(releaseDir)))
        {
            mReleaseInfo = new ReleaseInfo(branchInfo, releaseDir);
            mDbConnection = dbConnection;
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            var graph = new DependencyGraph<IComponent>();

            var prerequisites = new PrerequisitesComponent(mReleaseInfo);
            var tplComponent = new TemplatesComponent(mReleaseInfo);
            var reportsComponent = new ReportsComponent(mReleaseInfo);
            var scriptsComponent = new ScriptsComponent(mReleaseInfo, mDbConnection);

            graph.AddDependency(prerequisites, tplComponent);
            graph.AddDependency(prerequisites, reportsComponent);
            graph.AddDependency(prerequisites, scriptsComponent);
            graph.AddDependency(tplComponent, scriptsComponent);
            graph.AddDependency(reportsComponent, scriptsComponent);

            return graph.GetPath();
        }

        private class PrerequisitesComponent : ComponentBase
        {
            private readonly ReleaseInfo mReleaseInfo;

            public PrerequisitesComponent(ReleaseInfo releaseInfo)
            {
                mReleaseInfo = releaseInfo;
            }

            [RunAction(ActionConstants.MakeReleasePackage)]
            private IEnumerable<string> MakeReleasePackageRun(string action, ComponentRunContext runContext)
            {
                var packageDirectory = runContext.Config.GetPackageDirectory(mReleaseInfo);
                if (Directory.Exists(packageDirectory))
                {
                    yield return string.Format("Erasing contents of directory {0}", packageDirectory);
                    if (!runContext.DryRun)
                        FileUtils.DeleteDirectory(packageDirectory);
                }

                if (!Directory.Exists(packageDirectory))
                {
                    yield return string.Format("Creating directory {0}", packageDirectory);
                    if (!runContext.DryRun)
                        Directory.CreateDirectory(packageDirectory);
                }
            }
        }
    }
}