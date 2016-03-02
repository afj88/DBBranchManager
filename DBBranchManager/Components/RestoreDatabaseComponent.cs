using System.Collections.Generic;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class RestoreDatabaseComponent : ComponentBase
    {
        private readonly DatabaseInfo mDatabaseInfo;

        public RestoreDatabaseComponent(DatabaseInfo databaseInfo)
        {
            mDatabaseInfo = databaseInfo;
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            var script = string.Format(@"
USE [master]
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
RESTORE DATABASE [{0}] FROM  DISK = N'{1}' WITH  FILE = 1,  NOUNLOAD,  STATS = 5
ALTER DATABASE [{0}] SET MULTI_USER
", mDatabaseInfo.Name, mDatabaseInfo.BackupFilePath);

            yield return string.Format("Restoring {0}", mDatabaseInfo.Name);

            if (!runContext.DryRun)
            {
                using (var process = SqlUtils.SqlCmdExec(mDatabaseInfo.Connection, script))
                {
                    foreach (var processOutputLine in process.GetOutput())
                    {
                        yield return processOutputLine.Line;
                    }
                }
            }

            yield return "Database restore completed.";
        }
    }
}