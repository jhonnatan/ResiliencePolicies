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
    .New(10)                        // new with timeout 10
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
-----

### Putting all together
```sh    
public class MyClass // *** IMPORTANT: This class needs to be injected like Singleton
{
    private readonly IAsyncPolicy<HttpResponseMessage> policy;

    public MyClass()
    {
        // Configure in the constructor to not generate a lot of policies
        this.policy = new ResilientPolicy().New(10).WithRetry(6).WithCircuitBreaker(3, 5).WithBulkhead(2, int.MaxValue).Build(); 
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




### Requirements

* [Serilog] - It's necessary configure serilog to effective log retries, circuit breakers etc;


### Todos

 - Implement log without fix provider log;

Reference
----
    https://github.com/App-vNext/Polly
   
