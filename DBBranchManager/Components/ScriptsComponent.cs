﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class ScriptsComponent : ComponentBase
    {
        private static readonly Regex ScriptFileRegex = new Regex(@"^\d+(?:-(?<env>[^.]+))?\..*\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly string mScriptsPath;
        private readonly string mDeployPath;
        private readonly string mReleaseName;
        private readonly DatabaseConnectionInfo mDatabaseConnection;

        public ScriptsComponent(string scriptsPath, string deployPath, string releaseName, DatabaseConnectionInfo databaseConnection)
        {
            mScriptsPath = scriptsPath;
            mDeployPath = deployPath;
            mReleaseName = releaseName;
            mDatabaseConnection = databaseConnection;
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mScriptsPath))
            {
                yield return string.Format("Scripts: {0}", mScriptsPath);

                var sb = new StringBuilder();
                GenerateScript(runContext.Environment, sb, true).RunToEnd();

                var script = sb.ToString();
                yield return "Running script...";

                if (!runContext.DryRun)
                {
                    using (var sqlcmdResult = SqlUtils.SqlCmdExec(mDatabaseConnection, script))
                    {
                        runContext.IncreaseDepth();
                        foreach (var processOutputLine in sqlcmdResult.GetOutput())
                        {
                            if (processOutputLine.OutputType == ProcessOutputLine.OutputTypeEnum.StandardError)
                                yield return processOutputLine.Line;
                        }
                        runContext.DecreaseDepth();

                        if (sqlcmdResult.ExitCode != 0)
                        {
                            runContext.SetError();
                        }
                    }
                }
            }
        }

        [RunAction(ActionConstants.GenerateScripts)]
        private IEnumerable<string> GenerateScriptsRun(string action, ComponentRunContext context)
        {
            if (Directory.Exists(mScriptsPath))
            {
                var scriptFile = Path.Combine(mDeployPath, mReleaseName + @".sql");
                yield return string.Format("Generating {0}", scriptFile);

                var sb = new StringBuilder();
                foreach (var log in GenerateScript(context.Environment, sb, false))
                {
                    yield return string.Format("  {0}", log);
                }

                if (!context.DryRun)
                {
                    File.WriteAllText(scriptFile, sb.ToString());
                }
            }
        }

        private IEnumerable<string> GenerateScript(string environment, StringBuilder sb, bool commit)
        {
            sb.AppendFormat(@"
:on error exit
:setvar path ""{0}""

USE [DB_AVIVA_S2]
GO

SET XACT_ABORT ON
GO

BEGIN TRANSACTION
GO

TRUNCATE TABLE [Interdependencies].[TBC_CACHE_ITEM_DEPENDENCY]
", mScriptsPath);

            foreach (var file in FileUtils.EnumerateFiles(mScriptsPath, ScriptFileRegex.IsMatch))
            {
                var match = ScriptFileRegex.Match(file);
                if (match.Success && (!match.Groups["env"].Success || match.Groups["env"].Value == environment))
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
    }
}