using Polly;
using Serilog;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Component
{
    /// <summary>
    /// This class is using Decorator Pattern 
    /// See more : https://pt.wikipedia.org/wiki/Decorator
    /// </summary>
    public class ResilientPolicyComponent : IResilientPolicyComponent
    {
        
        private int timeoutSeconds;
        public virtual int[] HttpStatusCodeWorthRetry => new[] { 408, 429, 500, 502, 503, 504 };

        public virtual IAsyncPolicy<HttpResponseMessage> GetPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds);  // Configure timeout                                  
        }

        protected virtual void PolicyLog(string msg)
            => Log.Information(msg);

        public void SetParameters(int timeoutSeconds = 10)
            => this.timeoutSeconds = timeoutSeconds;

    }
}
