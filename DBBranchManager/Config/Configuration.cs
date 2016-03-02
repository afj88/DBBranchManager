using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Config
{
    internal class Configuration
    {
        public List<DatabaseConnectionInfo> DatabaseConnections { get; set; }
        public List<DatabaseInfo> Databases { get; set; }
        public List<BranchInfo> Branches { get; set; }
        public string ReleasePackagesPath { get; set; }
        public string ScriptsPath { get; set; }
        public string ActiveBranch { get; set; }
        public string BackupBranch { get; set; }
        public int ExecutionDelay { get; set; }
        public bool DryRun { get; set; }
        public string Environment { get; set; }
        public Dictionary<string, BeepInfo> Beeps { get; set; }

        public static Configuration LoadFromJson(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);

                var dbcs = jConfig["databaseConnections"].OfType<JProperty>().ToDictionary(x => x.Name, x => new DatabaseConnectionInfo
                {
                    Name = x.Name,
                    Server = (string)x.Value["server"],
                    User = (string)x.Value["user"],
                    Password = (string)x.Value["password"]
                });

                return new Configuration
                {
                    DatabaseConnections = dbcs.Values.ToList(),
                    Databases = jConfig["databases"].OfType<JProperty>().Select(x => new DatabaseInfo
                    {
                        Name = x.Name,
                        Connection = dbcs[(string)x.Value["connection"]],
                        BackupFilePath = (string)x.Value["backupFilePath"]
                    }).ToList(),
                    Branches = jConfig["branches"].OfType<JProperty>().Select(x => new BranchInfo
                    {
                        Name = x.Name,
                        BasePath = (string)x.Value["basePath"],
                        Parent = (string)x.Value["parent"],
                        DeployPath = (string)x.Value["deployPath"]
                    }).ToList(),
                    ReleasePackagesPath = (string)jConfig["releasePackagesPath"],
                    ScriptsPath = (string)jConfig["scriptsPath"] ?? (string)jConfig["releasePackagesPath"],
                    ActiveBranch = (string)jConfig["activeBranch"],
                    BackupBranch = (string)jConfig["backupBranch"],
                    ExecutionDelay = (int?)jConfig["executionDelay"] ?? 3000,
                    DryRun = (bool)jConfig["dryRun"],
                    Environment = (string)jConfig["environment"] ?? "dev",
                    Beeps = jConfig["beeps"].OfType<JProperty>().ToDictionary(x => x.Name, x => new BeepInfo(
                        (int?)x.Value["frequency"] ?? 800,
                        (int?)x.Value["length"] ?? 250,
                        (int?)x.Value["times"] ?? 1,
                        (float?)x.Value["dutyTime"] ?? 1))
                };
            }
        }
    }

    internal class BeepInfo
    {
        public BeepInfo(int frequency, int duration, int times, float dutyTime)
        {
            Frequency = frequency;
            Duration = duration;
            Times = times;
            DutyTime = dutyTime;
        }

        public int Frequency { get; private set; }
        public int Duration { get; private set; }
        public int Times { get; private set; }
        public float DutyTime { get; private set; }
    }
}