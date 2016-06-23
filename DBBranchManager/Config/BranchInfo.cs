namespace DBBranchManager.Config
{
    internal class BranchInfo
    {
        public string Name { get; set; }
        public string Parent { get; set; }
        public string BasePath { get; set; }
        public string DeployPath { get; set; }
        public string[] ReleasesToSkip { get; set; }
    }
}