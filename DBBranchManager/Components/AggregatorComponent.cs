using System.Collections.Generic;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal abstract class AggregatorComponent : IComponent
    {
        private string mLogPre;
        private string mLogPost;

        public AggregatorComponent() :
            this(null, null)
        {
        }

        public AggregatorComponent(string log) :
            this(string.Format("{0}: Begin", log), string.Format("{0}: End", log))
        {
        }

        public AggregatorComponent(string logPre, string logPost)
        {
            mLogPre = logPre;
            mLogPost = logPost;
        }

        public IEnumerable<string> Run(string action, ComponentRunContext runContext)
        {
            var pre = GetPreComponent(action, runContext);
            if (pre != null)
            {
                foreach (var log in pre.Run(action, runContext))
                {
                    yield return log;
                }
            }

            using (runContext.DepthScope())
            {
                foreach (var log in GetComponentsToRun(action, runContext).Run(action, runContext))
                {
                    yield return log;
                }
            }

            var post = GetPostComponent(action, runContext);
            if (post != null)
            {
                foreach (var log in post.Run(action, runContext))
                {
                    yield return log;
                }
            }
        }

        protected abstract IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext);

        protected virtual IComponent GetPreComponent(string action, ComponentRunContext runContext)
        {
            return mLogPre != null ? new LogComponent(mLogPre) : null;
        }

        protected virtual IComponent GetPostComponent(string action, ComponentRunContext runContext)
        {
            return mLogPost != null ? new LogComponent(mLogPost) : null;
        }


        protected string LogPre
        {
            get { return mLogPre; }
            set { mLogPre = value; }
        }

        protected string LogPost
        {
            get { return mLogPost; }
            set { mLogPost = value; }
        }
    }
}