using System;
using System.Collections.Generic;

namespace DBBranchManager.Components
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class RunActionAttribute : Attribute
    {
        private readonly HashSet<string> mHandledActions;

        public RunActionAttribute(params string[] handledActions)
        {
            mHandledActions = new HashSet<string>(handledActions);
        }

        public bool HandlesAction(string action)
        {
            return mHandledActions.Contains(action);
        }

        public IEnumerable<string> HandledActions
        {
            get { return mHandledActions; }
        }
    }
}