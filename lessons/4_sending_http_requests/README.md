# Sending HTTP Requests in ASP.NET Core with HttpClient

## Overview

This lesson covers how to use HttpClient in ASP.NET Core to send HTTP requests, process responses, handle errors, and implement retry policies.

## 1. Setting up HttpClient

### Dependency Injection Registration

In `Program.cs`, register HttpClient with the dependency injection container:

```csharp
// Basic registration
builder.Services.AddHttpClient();

// Named client with configuration
builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Typed client
builder.Services.AddHttpClient<IMyApiService, MyApiService>();
```

## 2. Important Classes

- **HttpClient**: Main class for sending HTTP requests
- **HttpRequestMessage**: Represents an HTTP request message
- **HttpResponseMessage**: Represents an HTTP response message
- **HttpContent**: Base class representing HTTP content (body)
- **StringContent**: Content as string
- **JsonContent**: Content as JSON (System.Text.Json)
- **StreamContent**: Content as stream

## 3. Injecting and Using HttpClient

### Method 1: Basic Injection

```csharp
public class MyService
{
    private readonly HttpClient _httpClient;

    public MyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetDataAsync()
    {
        var response = await _httpClient.GetAsync("https://api.example.com/data");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Method 2: Named Client

```csharp
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("MyApiClient");
        var response = await client.GetAsync("data");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

## 4. Sending Requests with Headers and Authorization

### Adding Headers

```csharp
public async Task<string> GetDataWithHeadersAsync()
{
    var client = _httpClientFactory.CreateClient();
    
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
    request.Headers.Add("X-Custom-Header", "custom-value");
    request.Headers.Add("User-Agent", "MyApp/1.0");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    
    var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}
```

### Adding Authorization Header

```csharp
public async Task<string> GetDataWithAuthAsync(string token)
{
    var client = _httpClientFactory.CreateClient();
    
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected-data");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}
```

## 5. Processing JSON Responses with System.Text.Json

### Model Classes

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiResponse<T>
{
    public T Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}
```

### JSON Processing Options

```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

### Reading JSON Responses

```csharp
public async Task<User> GetUserAsync(int userId)
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.GetAsync($"https://api.example.com/users/{userId}");
    
    response.EnsureSuccessStatusCode();
    
    var content = await response.Content.ReadAsStringAsync();
    var user = JsonSerializer.Deserialize<User>(content, jsonOptions);
    
    return user;
}

public async Task<List<User>> GetUsersAsync()
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.GetAsync("https://api.example.com/users");
    
    response.EnsureSuccessStatusCode();
    
    using var stream = await response.Content.ReadAsStreamAsync();
    var users = await JsonSerializer.DeserializeAsync<List<User>>(stream, jsonOptions);
    
    return users;
}
```

### Sending JSON Data

```csharp
public async Task<User> CreateUserAsync(User user)
{
    var client = _httpClientFactory.CreateClient();
    
    var jsonContent = JsonSerializer.Serialize(user, jsonOptions);
    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync("https://api.example.com/users", httpContent);
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var createdUser = JsonSerializer.Deserialize<User>(responseContent, jsonOptions);
    
    return createdUser;
}
```

## 6. Handling HTTP Response Properties

```csharp
public async Task ProcessResponseAsync()
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.GetAsync("https://api.example.com/data");
    
    // Check status code
    if (response.StatusCode == HttpStatusCode.OK)
    {
        // Process successful response
    }
    else if (response.StatusCode == HttpStatusCode.NotFound)
    {
        // Handle not found
    }
    
    // Check headers
    if (response.Headers.TryGetValues("X-RateLimit-Limit", out var rateLimitValues))
    {
        var rateLimit = rateLimitValues.FirstOrDefault();
    }
    
    // Check content type
    if (response.Content.Headers.ContentType?.MediaType == "application/json")
    {
        // Process as JSON
    }
    
    // Get response content
    var content = await response.Content.ReadAsStringAsync();
    var contentBytes = await response.Content.ReadAsByteArrayAsync();
    var contentStream = await response.Content.ReadAsStreamAsync();
}
```

## 7. Implementing Retry with Polly

### Install Required Packages
```bash
dotnet add package Microsoft.Extensions.Http.Polly
```

### Polly Policies Configuration

```csharp
// In Program.cs
using Polly;
using Polly.Extensions.Http;

// Configure retry policy with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError() // Handles 5xx, 408, and network failures
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Exponential backoff: 2, 4, 8 seconds
    );

// Configure circuit breaker policy
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)); // Break after 5 failures, wait 30 seconds

// Combine policies
var resiliencePipeline = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

// Register HttpClient with Polly policies
builder.Services.AddHttpClient("ResilientClient")
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy)
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

### Using Polly with Typed Clients

```csharp
public class ResilientApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public ResilientApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Define policy
        _resiliencePolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }

    public async Task<string> GetDataWithRetryAsync()
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync("data");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        });
    }
}
```

## 8. Complete Example with All Features

```csharp
public class CompleteApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public CompleteApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<User> GetUserWithRetryAndAuthAsync(int userId, string authToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null; // User not found
            }
            
            response.EnsureSuccessStatusCode();
            
            using var stream = await response.Content.ReadAsStreamAsync();
            var user = await JsonSerializer.DeserializeAsync<User>(stream, _jsonOptions);
            
            return user;
        });
    }

    public async Task<User> CreateUserWithRetryAsync(User user, string authToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var jsonContent = JsonSerializer.Serialize(user, _jsonOptions);
            using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "users")
            {
                Content = httpContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdUser = JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
            
            return createdUser;
        });
    }
}
```

## 9. Error Handling and Logging

```csharp
public class ErrorHandlingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(HttpClient httpClient, ILogger<ErrorHandlingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetDataSafelyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("data");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Request failed with status: {StatusCode}", response.StatusCode);
                
                // Handle specific status codes
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedAccessException("Authentication required");
                    case HttpStatusCode.Forbidden:
                        throw new AccessViolationException("Access denied");
                    case HttpStatusCode.NotFound:
                        return null;
                    default:
                        response.EnsureSuccessStatusCode();
                        break;
                }
            }
            
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw new ApplicationException("Service unavailable", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timed out");
            throw new TimeoutException("Request timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed");
            throw new ApplicationException("Invalid response format", ex);
        }
    }
}
```

## Best Practices

1. **Always use IHttpClientFactory** instead of creating HttpClient instances directly
2. **Implement retry logic** for transient failures using Polly
3. **Set appropriate timeouts** for different operations
4. **Use async/await** throughout to avoid blocking threads
5. **Dispose of HttpContent** and HttpResponseMessage properly
6. **Validate and sanitize** all inputs and outputs
7. **Implement proper error handling** and logging
8. **Use cancellation tokens** for long-running requests
9. **Consider rate limiting** when calling external APIs
10. **Secure sensitive data** like API keys and tokens

This comprehensive approach ensures robust HTTP communication in your ASP.NET Core applications.