using System.Collections;
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
                    Password = (string)x.Value["password"],
                    RelocatePath = (string)x.Value["relocatePath"],
                    Relocate = (bool?)x.Value["relocate"] ?? false
                });

                return new Configuration
                {
                    DatabaseConnections = dbcs.Values.ToList(),
                    Databases = jConfig["databases"].OfType<JProperty>().Select(x =>
                    {
                        var connection = dbcs[(string)x.Value["connection"]];
                        return new DatabaseInfo
                        {
                            Name = x.Name,
                            Connection = connection,
                            BackupFilePath = (string)x.Value["backupFilePath"],
                            RelocatePath = (string)x.Value["relocatePath"] ?? connection.RelocatePath,
                            Relocate = (bool?)x.Value["relocate"] ?? connection.Relocate
                        };
                    }).ToList(),
                    Branches = jConfig["branches"].OfType<JProperty>().Select(x => new BranchInfo
                    {
                        Name = x.Name,
                        BasePath = (string)x.Value["basePath"],
                        Parent = (string)x.Value["parent"],
                        DeployPath = (string)x.Value["deployPath"],
                        ReleasesToSkip = ToArray<string>(x.Value["releasesToSkip"])
                    }).ToList(),
                    ReleasePackagesPath = (string)jConfig["releasePackagesPath"],
                    ScriptsPath = (string)jConfig["scriptsPath"] ?? (string)jConfig["releasePackagesPath"],
                    ActiveBranch = (string)jConfig["activeBranch"],
                    BackupBranch = (string)jConfig["backupBranch"],
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

        private static T[] ToArray<T>(JToken token)
        {
            return token == null ? new T[0] : token.Select(x => x.Value<T>()).ToArray();
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