using System.Collections.Generic;
using DBBranchManager.Config;

namespace DBBranchManager.Components
{
    internal class ComponentRunContext
    {
        private readonly Configuration mConfig;
        private readonly string mEnvironment;
        private bool mError;
        private int mDepth;

        public ComponentRunContext(Configuration config) :
            this(config, config.Environment)
        {
        }

        public ComponentRunContext(Configuration config, string environment)
        {
            mConfig = config;
            mEnvironment = environment;
        }

        public Configuration Config
        {
            get { return mConfig; }
        }

        public bool DryRun
        {
            get { return mConfig.DryRun; }
        }

        public string Environment
        {
            get { return mEnvironment; }
        }

        public bool Error
        {
            get { return mError; }
        }

        public int Depth
        {
            get { return mDepth; }
        }

        public void SetError()
        {
            mError = true;
        }

        public void IncreaseDepth()
        {
            ++mDepth;
        }

        public void DecreaseDepth()
        {
            --mDepth;
        }
    }

    internal interface IComponent
    {
        IEnumerable<string> Run(string action, ComponentRunContext runContext);
    }
}