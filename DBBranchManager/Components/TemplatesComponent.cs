using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DBBranchManager.Constants;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class TemplatesComponent : ComponentBase
    {
        private static readonly Regex TemplateFileRegex = new Regex(@"^TPL_\d+_.+\.xls[mx]?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ReleaseInfo mReleaseInfo;
        private readonly string mTemplatesPath;

        public TemplatesComponent(ReleaseInfo releaseInfo)
        {
            mReleaseInfo = releaseInfo;
            mTemplatesPath = Path.Combine(releaseInfo.Path, "Templates");
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mTemplatesPath))
            {
                yield return string.Format("Templates {0} -> {1}", mTemplatesPath, mReleaseInfo.Branch.DeployPath);

                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mTemplatesPath, mReleaseInfo.Branch.DeployPath, TemplateFileRegex);
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
            if (Directory.Exists(mTemplatesPath))
            {
                var packageDir = runContext.Config.GetPackageDirectory(mReleaseInfo, "Reports+Templates");
                yield return string.Format("Templates {0} -> {1}", mTemplatesPath, packageDir);

                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mTemplatesPath, packageDir, TemplateFileRegex);
                    foreach (var log in synchronizer.Run(action, runContext))
                    {
                        yield return log;
                    }
                }
            }
        }
    }
}