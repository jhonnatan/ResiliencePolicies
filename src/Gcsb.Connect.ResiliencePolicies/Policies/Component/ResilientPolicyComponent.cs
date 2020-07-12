using Gcsb.Connect.ResiliencePolicies.Policies.Logger;
using Polly;
using System.Collections.Generic;
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
        private readonly ILoggerPolicy logger;

        public virtual int[] HttpStatusCodeWorthRetry => new[] { 408, 429, 500, 502, 503, 504 };
        public List<string> Logs { get; set; }

        public ResilientPolicyComponent(ILoggerPolicy logger)
        {
            this.logger = logger;
        }        

        public virtual IAsyncPolicy<HttpResponseMessage> GetPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds);  // Configure timeout                                  
        }

        protected virtual void PolicyLog(string msg)
            => logger.LogMessage(msg);

        public void SetParameters(int timeoutSeconds = 10)
            => this.timeoutSeconds = timeoutSeconds;

        public virtual List<string> GetLogs()
        {
            return this.Logs;
        }
    }
}
