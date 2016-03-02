using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class LogComponent : IComponent
    {
        private readonly string mLog;

        public LogComponent(string log)
        {
            mLog = log;
        }

        public IEnumerable<string> Run(string action, ComponentRunContext runContext)
        {
            yield return mLog;
        }
    }
}