using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Gcsb.Connect.ResiliencePolicies.Policies.Decorators;
using Polly;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies
{
    /// <summary>
    /// This class is using Builder Pattern
    /// See more in: https://pt.wikipedia.org/wiki/Builder
    /// </summary>
    public class ResilientPolicy
    {
        public ResilientPolicyComponent ResilientPolicyComponent { get; set; }

        public ResilientPolicy New(int timeout = 10)
        {
            ResilientPolicyComponent = new ResilientPolicyComponent();
            ResilientPolicyComponent.SetParameters(timeout);

            return new ResilientPolicy
            {
                ResilientPolicyComponent = ResilientPolicyComponent
            };
        }

        public ResilientPolicy WithRetry(int maxRetryCount)
        {
            var decorator = new ResilientPolicyRetry(ResilientPolicyComponent);
            decorator.SetRetryParameters(maxRetryCount);
            ResilientPolicyComponent = decorator;
            return this;
        }

        public ResilientPolicy WithCircuitBreaker(int maxExceptionsBeforeBreaking, int circuitBreakDurationSeconds)
        {
            var decorator = new ResilientPolicyCircuitBreaker(ResilientPolicyComponent);
            decorator.SetCircuitBreakerParameters(maxExceptionsBeforeBreaking, circuitBreakDurationSeconds);
            ResilientPolicyComponent = decorator;
            return this;
        }

        public ResilientPolicy WithBulkhead(int maxParallelizations, int maxQueuingActions = int.MaxValue)
        {
            var decorator = new ResilientPolicyBulkhead(ResilientPolicyComponent);
            decorator.SetBulkheadParameters(maxParallelizations, maxQueuingActions);
            ResilientPolicyComponent = decorator;
            return this;
        }

        public IAsyncPolicy<HttpResponseMessage> Build()
        {
            return ResilientPolicyComponent.GetPolicy();
        }
    }
}
