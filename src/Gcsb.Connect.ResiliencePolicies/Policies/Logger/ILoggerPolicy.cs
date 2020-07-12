namespace Gcsb.Connect.ResiliencePolicies.Policies.Logger
{
    /// <summary>
    /// Implement according to choice of log provider
    /// </summary>
    public interface ILoggerPolicy
    {
        void LogMessage(string msg);
    }
}
