using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBBranchManager.Config;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class BranchComponent : AggregatorComponent
    {
        private static readonly Regex ToDeployRegex = new Regex(@"^(?:to[ _]deploy).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly BranchInfo mBranchInfo;
        private readonly DatabaseConnectionInfo mDbConnection;

        public BranchComponent(BranchInfo branchInfo, DatabaseConnectionInfo dbConnection) :
            base(string.Format("Branch {0}", branchInfo.Name))
        {
            mBranchInfo = branchInfo;
            mDbConnection = dbConnection;
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mBranchInfo.BasePath))
            {
                var toSkip = new HashSet<string>(mBranchInfo.ReleasesToSkip, StringComparer.OrdinalIgnoreCase);
                var toDeployDirs = FileUtils.EnumerateDirectories2(mBranchInfo.BasePath, ToDeployRegex.IsMatch);

                foreach (var toDeployDir in toDeployDirs)
                {
                    if (toSkip.Contains(toDeployDir.FileName))
                    {
                        yield return new LogComponent(string.Format("Release '{0}': Skipped", toDeployDir.FileName));
                    }
                    else
                    {
                        yield return new ReleaseComponent(mBranchInfo, toDeployDir.FullPath, mDbConnection);
                    }
                }
            }
        }
    }
}