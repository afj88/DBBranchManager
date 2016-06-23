namespace DBBranchManager.Config
{
    internal class DatabaseConnectionInfo
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public string RelocatePath { get; set; }
        public bool Relocate { get; set; }
    }
}