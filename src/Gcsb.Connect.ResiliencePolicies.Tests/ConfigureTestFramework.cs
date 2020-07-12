using Autofac;
using Gcsb.Connect.ResiliencePolicies.Policies.Logger;
using Gcsb.Connect.ResiliencePolicies.Tests.LogMock;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("Gcsb.Connect.ResiliencePolicies.Tests.ConfigureTestFramework", "Gcsb.Connect.ResiliencePolicies.Tests")]
namespace Gcsb.Connect.ResiliencePolicies.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<ResiliencePolicies.Policies.ResilientPolicy>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<LoggerPolicy>().As<ILoggerPolicy>();
        }
    }
}
