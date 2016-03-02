using System;
using System.Collections.Generic;
using DBBranchManager.Components;

namespace DBBranchManager.Utils
{
    internal static class ComponentExtensions
    {
        public static IEnumerable<string> Run(this IEnumerable<IComponent> components, string action, ComponentRunContext runContext)
        {
            foreach (var component in components)
            {
                foreach (var log in component.Run(action, runContext))
                {
                    yield return log;

                    if (runContext.Error)
                        yield break;
                }
            }
        }

        public static IDisposable DepthScope(this ComponentRunContext runContext)
        {
            return new DepthScopeImpl(runContext);
        }

        private class DepthScopeImpl : IDisposable
        {
            private readonly ComponentRunContext mRunContext;

            public DepthScopeImpl(ComponentRunContext runContext)
            {
                mRunContext = runContext;
                runContext.IncreaseDepth();
            }

            public void Dispose()
            {
                mRunContext.DecreaseDepth();
            }
        }
    }
}