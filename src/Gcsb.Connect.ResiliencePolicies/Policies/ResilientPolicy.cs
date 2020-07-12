using Gcsb.Connect.ResiliencePolicies.Policies.Component;
using Gcsb.Connect.ResiliencePolicies.Policies.Decorators;
using Gcsb.Connect.ResiliencePolicies.Policies.Logger;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies
{
    /// <summary>
    /// This class is using Builder Pattern
    /// See more in: https://pt.wikipedia.org/wiki/Builder
    /// </summary>
    public class ResilientPolicy
    {
        private readonly ILoggerPolicy loggerPolicy;

        public static List<Tuple<TypePolicy, int[]>> Policies { get; set; }

        public ResilientPolicy(ILoggerPolicy loggerPolicy)
        {
            Policies = new List<Tuple<TypePolicy, int[]>>();
            this.loggerPolicy = loggerPolicy;
        }

        public ResilientPolicy WithTimeout(int timeout = 10)
        {            
            Policies.Add(new Tuple<TypePolicy, int[]>(TypePolicy.Timeout, new int[1] { timeout}));
            return this;
        }

        public ResilientPolicy WithRetry(int maxRetryCount)
        {             
            Policies.Add(new Tuple<TypePolicy, int[]>(TypePolicy.Retry, new int[1] { maxRetryCount }));
            return this;
        }

        public ResilientPolicy WithCircuitBreaker(int maxExceptionsBeforeBreaking, int circuitBreakDurationSeconds)
        {            
            Policies.Add(new Tuple<TypePolicy, int[]>(TypePolicy.CircuitBreaker, new int[2] { maxExceptionsBeforeBreaking, circuitBreakDurationSeconds }));
            return this;
        }

        public ResilientPolicy WithBulkhead(int maxParallelizations, int maxQueuingActions = int.MaxValue)
        {            
            Policies.Add(new Tuple<TypePolicy, int[]>(TypePolicy.Bulkhead, new int[2] { maxParallelizations, maxQueuingActions }));
            return this;
        }

        public IAsyncPolicy<HttpResponseMessage> Build()
        {
            // Create component
            var resilientPolicyComponent = new ResilientPolicyComponent(loggerPolicy);

            // Create decorators 
            // Set timeout defined our default
            if (Policies.Any(s => s.Item1 == TypePolicy.Timeout))
            {
                var parameters = Policies.Where(s => s.Item1 == TypePolicy.Timeout).Select(s => s.Item2).FirstOrDefault();
                resilientPolicyComponent.SetParameters(parameters[0]);
            }
            else
                resilientPolicyComponent.SetParameters(10); // default timeout


            // Set CircuitBreaker if it was defined
            if (Policies.Any(s=>s.Item1 == TypePolicy.CircuitBreaker))
            {
                var parameters = Policies.Where(s => s.Item1 == TypePolicy.CircuitBreaker).Select(s => s.Item2).FirstOrDefault();
                var decorator = new ResilientPolicyCircuitBreaker(loggerPolicy, resilientPolicyComponent);
                decorator.SetCircuitBreakerParameters(parameters[0], parameters[1]);
                resilientPolicyComponent = decorator;
            }

            // Set Retry if it was defined
            if (Policies.Any(s => s.Item1 == TypePolicy.Retry))
            {
                var parameters = Policies.Where(s => s.Item1 == TypePolicy.Retry).Select(s => s.Item2).FirstOrDefault();
                var decorator = new ResilientPolicyRetry(loggerPolicy, resilientPolicyComponent);
                decorator.SetRetryParameters(parameters[0]);
                resilientPolicyComponent = decorator;
            }

            // Set Bulkhead if it was defined
            if (Policies.Any(s => s.Item1 == TypePolicy.Bulkhead))
            {
                var parameters = Policies.Where(s => s.Item1 == TypePolicy.Bulkhead).Select(s => s.Item2).FirstOrDefault();
                var decorator = new ResilientPolicyBulkhead(loggerPolicy, resilientPolicyComponent);
                decorator.SetBulkheadParameters(parameters[0], parameters[1]);
                resilientPolicyComponent = decorator;
            }

            return resilientPolicyComponent.GetPolicy();
        }        
    }

    public enum TypePolicy
    {
        Timeout,
        CircuitBreaker,
        Retry,
        Bulkhead
    }
}
