using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Polly;
using Polly.Bulkhead;
using System;
using System.Linq;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Decorators
{
    public class ResilientPolicyRetry : ResilientPolicyDecorator
    {
        public int MaxRetryCount { get; private set; }

        public ResilientPolicyRetry(ResilientPolicyComponent component) : base(component) { }

        public override IAsyncPolicy<HttpResponseMessage> GetPolicy()
        {
            var waitAndRetryPolicy = 
                Policy.Handle<Exception>()
                .OrResult<HttpResponseMessage>(e => HttpStatusCodeWorthRetry.ToList().Contains((int)e.StatusCode))                
                .WaitAndRetryAsync(MaxRetryCount, // Retry N times with a delay between retries before ultimately giving up
                    attempt => TimeSpan.FromSeconds(1 * Math.Pow(2, attempt)), // Back off Exponential: 1, 2, 4, 8, 16, 32 ... (secs)                                                                                                      
                    (exception, calculatedWaitDuration) => PolicyLog($"API is throttling our requests automatically delaying for { calculatedWaitDuration.TotalMilliseconds}ms"));

            return waitAndRetryPolicy.WrapAsync(Component.GetPolicy());
        }

        public void SetRetryParameters(int maxRetryCount)
            => this.MaxRetryCount = maxRetryCount;
    }
}
