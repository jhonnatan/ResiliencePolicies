# Resilience Policies


The reason for creating this project is to abstract the creation of resilience policies using Polly to .Net:

  - Retry;
  - Circuit Breaker;
  - Bulkhead;
  - Timeout;
-----

This solution was implemented using two design patterns, Decorator and Builder.
To configure and use:

```sh
// Creating a policy - Needs to injected like a Singleton to share this policy
var policy = new ResilientPolicy()
    .WithTimeout(10)                // With timeout 10
    .WithRetry(6)                   // With exponential retry x times - 1, 2, 4, 8, 16, 32 ...(secs)
    .WithCircuitBreaker(3, 5)       // Break in 3 exception for 5 seconds
    .WithBulkhead(2, int.MaxValue)  // Parallelization of two with infinite queue
    .Build();                       // Create, get a instance of policy
```

```sh
// Request using the created policy using Flur - Flur is not required
var response = await policy.ExecuteAndCaptureAsync(() =>
    "http://api.foo.com"
    .AllowAnyHttpStatus()
    .GetAsync());
```  

```sh
// Implement interface ILoggerPolicy, according to choice of log provider 
 public class LoggerPolicy : ILoggerPolicy
    {
        public void LogMessage(string msg)
        { 
            // Your logger choice here
        }
    }
-----
```


### Putting all together
```sh    
public class MyClient // *** IMPORTANT: This class needs to be injected like Singleton
{
    private readonly IAsyncPolicy<HttpResponseMessage> policy;

    public MyClient(ResilientPolicy resilientPolicy)
    {        
        // Configure in the constructor to not generate a lot of policies
        this.policy = resilientPolicy.WithTimeout(10).WithRetry(6).WithCircuitBreaker(3, 5).WithBulkhead(2, int.MaxValue).Build(); 
    }

    public void DoRequest()
    {
        var response = policy.ExecuteAndCaptureAsync(() =>
            "http://api.foo.com"
            .AllowAnyHttpStatus()
            .GetAsync());

        // serialize response . . .
    }
}
```
```sh   
// Concrete Logger  - This class will be injected in MyClient constructor
public class LoggerPolicy : ILoggerPolicy
{
    private readonly ILogWriteOnlyRepository logWriteOnlyRepository;

    public LoggerPolicy(ILogWriteOnlyRepository logWriteOnlyRepository)
    {
        this.logWriteOnlyRepository = logWriteOnlyRepository;
    }

    public void LogMessage(string msg)
    {
        // using local log - You can call serilog here if you want or any provider log
        logWriteOnlyRepository.Add(Log.CreateProcessingLog("ServiceClient", msg));        
    }
}

```

```sh   
// If you use Autofac, configure like this
builder.RegisterType<LoggerPolicy>().As<ILoggerPolicy>().InstancePerLifetimeScope(); 
builder.RegisterType<ResiliencePolicies.Policies.ResilientPolicy>().AsSelf().InstancePerLifetimeScope();            
builder.RegisterType<MyClient>().As<IMyClient>().AsSelf().SingleInstance(); // Singleton here
```

Reference
----
    https://github.com/App-vNext/Polly
   
