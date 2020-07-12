using Gcsb.Connect.ResiliencePolicies.Policies.Logger;

namespace Gcsb.Connect.ResiliencePolicies.Tests.LogMock
{
    /// <summary>
    /// Mock LogInterface to test component and decorators
    /// </summary>
    public class LoggerPolicy : ILoggerPolicy
    {
        public void LogMessage(string msg){ }
    }
}
