using Gcsb.Connect.ResiliencePolicies.Policies.Component;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Decorators
{
    public abstract class ResilientPolicyDecorator : ResilientPolicyComponent
    {
        protected ResilientPolicyComponent Component { get; private set; }

        public ResilientPolicyDecorator(ResilientPolicyComponent component)
        {
            this.Component = component;
        }
    }
}
