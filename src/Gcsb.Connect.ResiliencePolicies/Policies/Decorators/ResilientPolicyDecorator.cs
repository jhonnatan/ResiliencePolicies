using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Gcsb.Connect.ResiliencePolicies.Policies.Logger;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Decorators
{
    public abstract class ResilientPolicyDecorator : ResilientPolicyComponent
    {        
        protected ResilientPolicyComponent Component { get; private set; }
        public ResilientPolicyDecorator(ILoggerPolicy logger, ResilientPolicyComponent component) : base(logger)
        {
            this.Component = component;            
        }
    }
}
