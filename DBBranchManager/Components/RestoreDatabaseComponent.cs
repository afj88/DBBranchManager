using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Utils.Sql;

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
            var sb = new StringBuilder();
            var dbName = SqlUtils.ToBracketedIdentifier(mDatabaseInfo.Name);
            var sqlParams = new SqlParamCollection();

            sb.AppendFormat(
                "USE [master]\n" +
                "IF db_id(@dbName) IS NOT NULL\n" +
                "  ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                "RESTORE DATABASE {0} FROM DISK = @backupFile WITH FILE = 1, NOUNLOAD, STATS = 5",
                dbName);

            sqlParams.Add("@dbName", SqlDbType.NVarChar).Value = mDatabaseInfo.Name;
            sqlParams.Add("@backupFile", SqlDbType.NVarChar).Value = mDatabaseInfo.BackupFilePath;

            if (mDatabaseInfo.Relocate)
            {
                var names = SqlUtils.GetLogicalAndPhysicalNamesFromBackupFile(mDatabaseInfo.Connection, mDatabaseInfo.BackupFilePath);

                var i = 0;
                foreach (var tuple in names)
                {
                    var logical = string.Format("@l{0}", i);
                    var physical = string.Format("@p{0}", i);
                    sb.AppendFormat(",\n  MOVE {0} TO {1}", logical, physical);

                    var destPath = GetRelocation(tuple.Item2);
                    sqlParams.Add(logical, SqlDbType.NVarChar).Value = tuple.Item1;
                    sqlParams.Add(physical, SqlDbType.NVarChar).Value = destPath;

                    ++i;
                }
            }

            sb.AppendFormat(
                "\nALTER DATABASE {0} SET MULTI_USER\n", dbName);

            yield return string.Format("Restoring {0}", mDatabaseInfo.Name);

            if (!runContext.DryRun)
            {
                using (var process = SqlUtils.Exec(mDatabaseInfo.Connection, sb.ToString(), sqlParams))
                {
                    foreach (var processOutputLine in process.GetOutput())
                    {
                        yield return processOutputLine.Line;
                    }
                }
            }

            yield return "Database restore completed.";
        }

        private string GetRelocation(string physical)
        {
            return Path.Combine(mDatabaseInfo.RelocatePath, string.Format("{0}{1}", mDatabaseInfo.Name, Path.GetExtension(physical)));
        }
    }
}