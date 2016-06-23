using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Utils;
using DBBranchManager.Utils.Sql;

namespace DBBranchManager.Components
{
    internal class ScriptsComponent : ComponentBase
    {
        private static readonly Regex ScriptFileRegex = new Regex(@"^\d+(?:-(?<env>[^.]+))?\..*\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ReleaseInfo mReleaseInfo;
        private readonly string mScriptsPath;
        private readonly DatabaseConnectionInfo mDatabaseConnection;


        public ScriptsComponent(ReleaseInfo releaseInfo, DatabaseConnectionInfo dbConnection)
        {
            mReleaseInfo = releaseInfo;
            mScriptsPath = Path.Combine(releaseInfo.Path, "Scripts");
            mDatabaseConnection = dbConnection;
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mScriptsPath))
            {
                yield return string.Format("Scripts: {0}", mScriptsPath);

                var sb = new StringBuilder();
                GenerateScript(runContext.Environment, sb, true, mScriptsPath).RunToEnd();

                var script = sb.ToString();
                yield return "Running script...";

                if (!runContext.DryRun)
                {
                    using (var sqlcmdResult = SqlUtils.SqlCmdExec(mDatabaseConnection, script))
                    using (runContext.DepthScope())
                    {
                        foreach (var processOutputLine in sqlcmdResult.GetOutput())
                        {
                            if (processOutputLine.OutputType == ProcessOutputLine.OutputTypeEnum.StandardError)
                                yield return processOutputLine.Line;
                        }

                        if (sqlcmdResult.ExitCode != 0)
                        {
                            runContext.SetError();
                        }
                    }
                }
            }
        }

        [RunAction(ActionConstants.GenerateScripts)]
        private IEnumerable<string> GenerateScriptsRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mScriptsPath))
            {
                if (!Directory.Exists(runContext.Config.ScriptsPath))
                {
                    yield return string.Format("Creating directory {0}", runContext.Config.ScriptsPath);
                    if (!runContext.DryRun)
                        Directory.CreateDirectory(runContext.Config.ScriptsPath);
                }

                var scriptFile = Path.Combine(runContext.Config.ScriptsPath, mReleaseInfo.Branch.Name + "$" + mReleaseInfo.Name + @".sql");
                yield return string.Format("Generating {0}", scriptFile);

                var sb = new StringBuilder();
                using (runContext.DepthScope())
                {
                    foreach (var log in GenerateScript(runContext.Environment, sb, false, mScriptsPath))
                    {
                        yield return log;
                    }
                }

                if (!runContext.DryRun)
                {
                    File.WriteAllText(scriptFile, sb.ToString());
                }
            }
        }

        [RunAction(ActionConstants.MakeReleasePackage)]
        private IEnumerable<string> MakeReleasePackageRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mScriptsPath))
            {
                var packageDir = runContext.Config.GetPackageDirectory(mReleaseInfo, "Scripts");
                yield return string.Format("Scripts {0} -> {1}", mScriptsPath, packageDir);

                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mScriptsPath, packageDir, GetScriptsFilterByEnvironment(runContext.Environment));
                    foreach (var log in synchronizer.Run(action, runContext))
                    {
                        yield return log;
                    }
                }

                var scriptName = mReleaseInfo.Name + @".sql";
                yield return string.Format("Generating script {0}", scriptName);

                var sb = new StringBuilder();
                GenerateScript(runContext.Environment, sb, false, packageDir).RunToEnd();

                if (!runContext.DryRun)
                {
                    File.WriteAllText(runContext.Config.GetPackageDirectory(mReleaseInfo.Branch, scriptName), sb.ToString());
                }
            }
        }

        private IEnumerable<string> GenerateScript(string environment, StringBuilder sb, bool commit, object scriptsPath)
        {
            sb.AppendFormat(":on error exit\n" +
                            ":setvar path \"{0}\"\n" +
                            "\n" +
                            "USE [DB_AVIVA_S2]\n" +
                            "GO\n" +
                            "\n" +
                            "SET XACT_ABORT ON\n" +
                            "GO\n" +
                            "\n" +
                            "BEGIN TRANSACTION\n" +
                            "GO\n" +
                            "\n" +
                            "TRUNCATE TABLE [Interdependencies].[TBC_CACHE_ITEM_DEPENDENCY]\n",
                scriptsPath);

            var filter = GetScriptsFilterByEnvironment(environment);
            foreach (var file in FileUtils.EnumerateFiles(mScriptsPath, ScriptFileRegex.IsMatch))
            {
                if (filter(file))
                {
                    sb.AppendFormat("\nPRINT 'BEGIN {0}'\nGO\n:r $(path)\\\"{0}\"\nGO\nPRINT 'END {0}'", file);
                    yield return string.Format("Adding {0}", file);
                }
                else
                {
                    yield return string.Format("Skipping {0}", file);
                }
            }

            if (commit)
            {
                yield return "Adding Commit...";
                sb.Append("\nGO\n\nPRINT 'Committing...'\n--ROLLBACK TRANSACTION\nCOMMIT TRANSACTION");
            }
            else
            {
                yield return "Adding Rollback...";
                sb.Append("\nGO\n\nPRINT 'Rolling Back...'\nROLLBACK TRANSACTION\n--COMMIT TRANSACTION");
            }
        }

        private static Func<string, bool> GetScriptsFilterByEnvironment(string environment)
        {
            return file =>
            {
                var match = ScriptFileRegex.Match(file);
                return !match.Groups["env"].Success || match.Groups["env"].Value == environment;
            };
        }
    }
}