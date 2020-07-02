using Polly;
using System.Net.Http;

namespace Gcsb.Connect.ResiliencePolicies.Policies.Component
{
    public interface IResilientPolicyComponent
    {
        IAsyncPolicy<HttpResponseMessage> GetPolicy();
    }
}
