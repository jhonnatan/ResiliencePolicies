using FluentAssertions;
using Gcsb.Connect.ResiliencePolicies.Policies;
using Xunit;
using Flurl.Http;
using Flurl.Http.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit.Frameworks.Autofac;
using System.Threading;

namespace Gcsb.Connect.ResiliencePolicies.Tests.Policies
{
    [UseAutofacTestFramework]
    public class PoliciesTests
    {        
        private readonly ResilientPolicy resilientPolicyBuilder;

        public PoliciesTests(ResilientPolicy resilientPolicyBuilder)
        {            
            this.resilientPolicyBuilder = resilientPolicyBuilder;
        }

        [Fact]
        public void ShouldCreatePolicy()
        {
            ResilientPolicy resilientPolicy = resilientPolicyBuilder;
            var policy = resilientPolicy.WithTimeout(10).WithRetry(3).Build();            
            policy.Should().NotBeNull();
        }

        [Fact]
        public async void ShouldExecuteWith3Retries()
        {
            using (var httpTest = new HttpTest())
            {
                // arrange
                httpTest
                    .RespondWith("API is throttling", 500) //1
                    .RespondWith("API is throttling", 500) //2
                    .RespondWith("API is throttling", 500) //3
                    .RespondWith("API is throttling", 500); //4

                // Act
                var policy = resilientPolicyBuilder.WithTimeout(10).WithRetry(3).Build();
                await policy.ExecuteAndCaptureAsync(() =>
                                        "http://api.foo.com"
                                        .AllowAnyHttpStatus()
                                        .GetAsync());

                //Assert
                httpTest.ShouldHaveCalled("http://api.foo.com")
                    .WithVerb(HttpMethod.Get)
                    .Times(4);
            }
        }

        [Fact]
        public async void ShouldExecuteWith3RetriesButGetSucessIn2()
        {
            using (var httpTest = new HttpTest())
            {
                // Arrange
                httpTest
                    .RespondWith("InternalServerError - API is throttling", 500)
                    .RespondWith("InternalServerError - API is throttling", 500)
                    .RespondWith("Sucesso", 200);

                // Act
                var policy = resilientPolicyBuilder.WithTimeout(10).WithRetry(3).Build();
                var response = await policy.ExecuteAndCaptureAsync(() =>
                                        "http://api.foo.com"
                                        .AllowAnyHttpStatus()
                                        .GetAsync());

                //Assert
                httpTest.ShouldHaveCalled("http://api.foo.com")
                    .WithVerb(HttpMethod.Get)
                    .Times(3);
                response.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async void ShouldExecuteWith5RetriesAndCircuitBreaker()
        {
            using (var httpTest = new HttpTest())
            {
                // arrange
                httpTest
                    .RespondWith("API is throttling", 500) // First
                    .RespondWith("API is throttling", 500) // Retries
                    .RespondWith("Break circuit", 500)
                    .RespondWith("API is throttling", 500)
                    .RespondWith("API is throttling", 500)
                    .RespondWith("API is throttling", 500);

                // Act
                //var policy = resilientPolicyBuilder.WithTimeout(10).WithRetry(5).WithCircuitBreaker(3, 10).Build();
                var policy = resilientPolicyBuilder.WithTimeout(10).WithCircuitBreaker(3, 3).WithRetry(5).Build();
                var response = await policy.ExecuteAndCaptureAsync(() =>
                                        "http://api.foo.com"
                                        .AllowAnyHttpStatus()
                                        .GetAsync());

                //Assert
                httpTest.ShouldHaveCalled("http://api.foo.com")
                    .WithVerb(HttpMethod.Get)
                    .Times(6);
            }
        }

        [Fact]
        public void ShouldExecute50RequestWith2ParallelizationsWithBulkhead()
        {
            using (var httpTest = new HttpTest())
            {
                // arrange
                httpTest.RespondWith("OK", 200); // always return 200                    

                // Act
                var policy = resilientPolicyBuilder.WithTimeout(10).WithBulkhead(2, int.MaxValue).Build();

                Task<Polly.PolicyResult<HttpResponseMessage>>[] tasks = new Task<Polly.PolicyResult<HttpResponseMessage>>[50];

                for (int i = 0; i <= 49; i++)
                {
                    int count = i;
                    Thread.Sleep(100);
                    tasks[count] = Task.Run(() => policy.ExecuteAndCaptureAsync(() => "http://api.foo.com".AllowAnyHttpStatus().GetAsync()));
                }
                Task.WaitAll(tasks);

                var responses = new List<Polly.PolicyResult<HttpResponseMessage>>();
                foreach (var task in tasks)
                {
                    responses.Add(task.Result);
                    task.Dispose();
                }

                //Assert
                httpTest.ShouldHaveCalled("http://api.foo.com").WithVerb(HttpMethod.Get).Times(50);
            }
        }

        [Fact]
        public void ShouldExecute10RequestWith2ParallelizationsWithBulkheadAndRetry()
        {
            using (var httpTest = new HttpTest())
            {
                // arrange
                httpTest
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200);

                // Act
                var policy = resilientPolicyBuilder.WithTimeout(10).WithRetry(5).WithBulkhead(2, int.MaxValue).Build();
                Task<Polly.PolicyResult<HttpResponseMessage>>[] tasks = new Task<Polly.PolicyResult<HttpResponseMessage>>[10];

                for (int i = 0; i <= 9; i++)
                {
                    int count = i;
                    Thread.Sleep(100);
                    tasks[count] = Task.Run(() => policy.ExecuteAndCaptureAsync(() => "http://api.foo.com".AllowAnyHttpStatus().GetAsync()));
                }
                Task.WaitAll(tasks);

                var responses = new List<Polly.PolicyResult<HttpResponseMessage>>();
                foreach (var task in tasks)
                {
                    responses.Add(task.Result);
                    task.Dispose();
                }

                //Assert
                responses.Any(s => s.Result.StatusCode != System.Net.HttpStatusCode.OK).Should().BeFalse();
            }
        }

        [Fact]
        public void ShouldExecute10RequestWithSucessUsingAllResiliencePolicies()
        {
            using (var httpTest = new HttpTest())
            {
                // arrange
                httpTest
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Service Unavailable", 503)
                    .RespondWith("Internal Server Error", 429)
                    .RespondWith("OK", 200)
                    .RespondWith("Bad Gateway", 502)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Timeout", 408)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Timeout", 408)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Service Unavailable", 503)
                    .RespondWith("Internal Server Error", 429)
                    .RespondWith("OK", 200)
                    .RespondWith("Bad Gateway", 502)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("Timeout", 408)
                    .RespondWith("Timeout", 408)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("Internal Server Error", 500)
                    .RespondWith("OK", 200)
                    .RespondWith("OK", 200);                    

                // Act
                var policy = resilientPolicyBuilder.WithTimeout(10).WithRetry(6).WithCircuitBreaker(3, 5).WithBulkhead(2, int.MaxValue).Build();
                Task<Polly.PolicyResult<HttpResponseMessage>>[] tasks = new Task<Polly.PolicyResult<HttpResponseMessage>>[10];

                for (int i = 0; i <= 9; i++)
                {
                    int count = i;
                    Thread.Sleep(100);
                    tasks[count] = Task.Run(() => policy.ExecuteAndCaptureAsync(() => "http://api.foo.com".AllowAnyHttpStatus().GetAsync()));
                }
                Task.WaitAll(tasks);

                var responses = new List<Polly.PolicyResult<HttpResponseMessage>>();
                foreach (var task in tasks)
                {
                    responses.Add(task.Result);
                    task.Dispose();
                }

                //Assert
                responses.Any(s => s.Result.StatusCode != System.Net.HttpStatusCode.OK).Should().BeFalse();
            }
        }
    }
}
