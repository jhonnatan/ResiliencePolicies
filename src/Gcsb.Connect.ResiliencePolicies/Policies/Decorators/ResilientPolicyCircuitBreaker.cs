using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Polly;
using System;
using System.Linq;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Decorators
{
    public class ResilientPolicyCircuitBreaker : ResilientPolicyDecorator
    {
        public int MaxExceptionsBeforeBreaking { get; private set; }
        public int CircuitBreakDurationSeconds { get; private set; }
        public ResilientPolicyCircuitBreaker(ResilientPolicyComponent component) : base(component) { }

        public override IAsyncPolicy<HttpResponseMessage> GetPolicy()
        {
            var circuitBreakerPolicyForRecoverable = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(r => HttpStatusCodeWorthRetry.ToList().Contains((int)r.StatusCode))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: MaxExceptionsBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(CircuitBreakDurationSeconds),
                    onBreak: (outcome, breakDelay) => PolicyLog($"Polly Circuit Breaker logging: Breaking the circuit for {breakDelay.TotalMilliseconds}ms due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}"),
                    onReset: () => PolicyLog($"Polly Circuit Breaker logging: Call ok... closed the circuit again"),
                    onHalfOpen: () => PolicyLog($"Polly Circuit Breaker logging: Half-open: Next call is a trial"));

            return circuitBreakerPolicyForRecoverable.WrapAsync(Component.GetPolicy());
        }

        public void SetCircuitBreakerParameters(int maxExceptionsBeforeBreaking, int circuitBreakDurationSeconds)
        {
            this.MaxExceptionsBeforeBreaking = maxExceptionsBeforeBreaking;
            this.CircuitBreakDurationSeconds = circuitBreakDurationSeconds;
        }
    }
}
