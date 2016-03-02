using System.IO;
using DBBranchManager.Config;

namespace DBBranchManager.Utils
{
    internal static class SqlUtils
    {
        public static IProcessExecutionResult SqlCmdExec(DatabaseConnectionInfo db, string script)
        {
            var args = string.Format(@"-r 1 -S ""{0}"" -U ""{1}"" -P ""{2}""", db.Server, db.User, db.Password);
            using (var input = new MemoryStream())
            {
                var writer = new StreamWriter(input);
                writer.WriteLine(script);
                writer.WriteLine("GO");
                writer.WriteLine("exit");
                writer.Flush();

                input.Position = 0;

                return ProcessUtils.Exec("sqlcmd", args, input);
            }
        }
    }
}