namespace DBBranchManager.Config
{
    internal class DatabaseInfo
    {
        public string Name { get; set; }
        public DatabaseConnectionInfo Connection { get; set; }
        public string BackupFilePath { get; set; }

        public string RelocatePath { get; set; }
        public bool Relocate { get; set; }
    }
}