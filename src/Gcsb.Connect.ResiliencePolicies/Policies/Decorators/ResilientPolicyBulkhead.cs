using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Gcsb.Connect.ResiliencePolicies.Policies.Logger;
using Polly;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Decorators
{
    /// <summary>
    /// Bulkhead is to limit the resources consumable by the governed actions, such that a fault 'storm' cannot cause a cascading failure also bringing down other operations.
    /// See more about Bulkhead in : https://github.com/App-vNext/Polly/wiki/Bulkhead
    /// </summary>
    public class ResilientPolicyBulkhead : ResilientPolicyDecorator
    {
        public int MaxParallelizations { get; private set; }
        public int MaxQueuingActions { get; private set; }
        public ResilientPolicyBulkhead(ILoggerPolicy logger, ResilientPolicyComponent component) : base(logger, component) { }

        public override IAsyncPolicy<HttpResponseMessage> GetPolicy()
        {
            var sharedBulkhead = Policy.BulkheadAsync(MaxParallelizations, MaxQueuingActions);
            return sharedBulkhead.WrapAsync(Component.GetPolicy());
        }

        public void SetBulkheadParameters(int maxParallelizations, int maxQueuingActions = int.MaxValue)
        {
            this.MaxParallelizations = maxParallelizations;
            this.MaxQueuingActions = maxQueuingActions;
        }
    }
}
