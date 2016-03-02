using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DBBranchManager.Constants;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class ReportsComponent : ComponentBase
    {
        private static readonly Regex ReportFileRegex = new Regex(@"^[DTX]_\d+.+\.x(?:lsm|ml)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ReleaseInfo mReleaseInfo;
        private readonly string mReportsPath;

        public ReportsComponent(ReleaseInfo releaseInfo)
        {
            mReleaseInfo = releaseInfo;
            mReportsPath = Path.Combine(releaseInfo.Path, "Reports");
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mReportsPath))
            {
                yield return string.Format("Reports: {0} -> {1}", mReportsPath, mReleaseInfo.Branch.DeployPath);

                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mReportsPath, mReleaseInfo.Branch.DeployPath, ReportFileRegex);
                    foreach (var log in synchronizer.Run(action, runContext))
                    {
                        yield return log;
                    }
                }
            }
        }

        [RunAction(ActionConstants.MakeReleasePackage)]
        private IEnumerable<string> MakeReleasePackageRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mReportsPath))
            {
                var packageDir = runContext.Config.GetPackageDirectory(mReleaseInfo, "Reports+Templates");
                yield return string.Format("Reports {0} -> {1}", mReportsPath, packageDir);

                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mReportsPath, packageDir, ReportFileRegex);
                    foreach (var log in synchronizer.Run(action, runContext))
                    {
                        yield return log;
                    }
                }
            }
        }
    }
}