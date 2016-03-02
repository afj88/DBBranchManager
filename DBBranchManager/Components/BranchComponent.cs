using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBBranchManager.Config;

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
                var toDeployDirs = Directory.EnumerateDirectories(mBranchInfo.BasePath)
                    .Where(x => ToDeployRegex.IsMatch(Path.GetFileName(x)));

                foreach (var toDeployDir in toDeployDirs)
                {
                    yield return new ReleaseComponent(mBranchInfo, toDeployDir, mDbConnection);
                }
            }
        }
    }
}