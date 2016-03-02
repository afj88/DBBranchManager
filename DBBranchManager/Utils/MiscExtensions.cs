using System.IO;
using System.Linq;
using DBBranchManager.Components;
using DBBranchManager.Config;

namespace DBBranchManager.Utils
{
    internal static class MiscExtensions
    {
        public static string GetPackageDirectory(this Configuration config, params string[] paths)
        {
            return Path.Combine(new[] { config.ReleasePackagesPath }.Union(paths).ToArray());
        }

        public static string GetPackageDirectory(this Configuration config, BranchInfo branch, params string[] paths)
        {
            return config.GetPackageDirectory(new[] { config.ReleasePackagesPath, branch.Name }.Union(paths).ToArray());
        }

        public static string GetPackageDirectory(this Configuration config, ReleaseInfo release, params string[] paths)
        {
            return config.GetPackageDirectory(release.Branch, new[] { release.Name }.Union(paths).ToArray());
        }
    }
}