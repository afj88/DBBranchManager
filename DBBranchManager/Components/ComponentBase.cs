using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DBBranchManager.Components
{
    internal abstract class ComponentBase : IComponent
    {
        private readonly Dictionary<string, Func<string, ComponentRunContext, IEnumerable<string>>> mRunners;

        protected ComponentBase()
        {
            mRunners = new Dictionary<string, Func<string, ComponentRunContext, IEnumerable<string>>>();
            RegisterActionsFromAttributes();
        }

        protected void RegisterHandler(string action, Func<string, ComponentRunContext, IEnumerable<string>> runner)
        {
            mRunners.Add(action, runner);
        }

        private void RegisterActionsFromAttributes()
        {
            foreach (var methodInfo in GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var runActionAttribute in methodInfo.GetCustomAttributes<RunActionAttribute>())
                {
                    foreach (var handledAction in runActionAttribute.HandledActions)
                    {
                        RegisterHandler(handledAction, MakeInvokeFunction(methodInfo));
                    }
                }
            }
        }

        private Func<string, ComponentRunContext, IEnumerable<string>> MakeInvokeFunction(MethodInfo methodInfo)
        {
            var self = Expression.Convert(Expression.Constant(this), methodInfo.DeclaringType);
            var actionParam = Expression.Parameter(typeof(string));
            var contextParam = Expression.Parameter(typeof(ComponentRunContext));
            var call = Expression.Call(self, methodInfo, actionParam, contextParam);
            var lambda = Expression.Lambda<Func<string, ComponentRunContext, IEnumerable<string>>>(call, actionParam, contextParam);
            return lambda.Compile();
        }

        public IEnumerable<string> Run(string action, ComponentRunContext runContext)
        {
            Func<string, ComponentRunContext, IEnumerable<string>> runner;
            if (mRunners.TryGetValue(action, out runner))
            {
                return runner(action, runContext);
            }

            return Enumerable.Empty<string>();
        }
    }
}