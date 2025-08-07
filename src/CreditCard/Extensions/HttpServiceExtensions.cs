using CreditCard.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Globalization;
using System.Net.Sockets;
using System.Threading.RateLimiting;

namespace CreditCard.Extensions
{
    public static class HttpServiceExtensions
    {
        private const int DefaultRetryInterval = 1;
        private const string RetryKey = "IsRetry";

        public static IHttpClientBuilder AddHttpClientWithPolicies<TContract, TImplementation, TConfig>(
            this IServiceCollection services,
            IConfigurationSection configurationSection,
            string name = null)
            where TContract : class
            where TImplementation : class, TContract
            where TConfig : class, IHttpClientConfig, IHttpPolicyConfig, new() =>
            AddHttpClientWithPolicies<TContract, TImplementation, TConfig>(services, configurationSection, name, true);

        public static IHttpClientBuilder AddHttpClientWithPoliciesNoPostMethodRetries<TContract, TImplementation, TConfig>(
            this IServiceCollection services,
            IConfigurationSection configurationSection,
            string name = null)
            where TContract : class
            where TImplementation : class, TContract
            where TConfig : class, IHttpClientConfig, IHttpPolicyConfig, new() =>
            AddHttpClientWithPolicies<TContract, TImplementation, TConfig>(services, configurationSection, name, false);

        private static IHttpClientBuilder AddHttpClientWithPolicies<TContract, TImplementation, TConfig>(
          this IServiceCollection services,
          IConfiguration configurationSection,
          string name,
          bool retryAllHttpMethodsRequests)
          where TContract : class
          where TImplementation : class, TContract
          where TConfig : class, IHttpClientConfig, IHttpPolicyConfig, new()
        {
            var clientConfig = configurationSection.Get<TConfig>() ?? new TConfig();
            services.TryAddScoped<ApplicationInsightsPayloadTracerHandler>();

            var httpBuilder = (!string.IsNullOrEmpty(name)
                ? services.AddHttpClient<TContract, TImplementation>(name)
                : services.AddHttpClient<TContract, TImplementation>())
                    .ConfigureHttpClient((sp, httpClient) =>
                    {
                        if (clientConfig.BaseEndpoint != null)
                        {
                            httpClient.BaseAddress = clientConfig.BaseEndpoint;
                        }

                        if (clientConfig.RequestTimeout.TotalSeconds > 0)
                        {
                            httpClient.Timeout = clientConfig.RequestTimeout;
                        }
                    });

            httpBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                UseCookies = clientConfig.DisableCookies
            });

            if (clientConfig.HttpPolicyConfig != null)
            {
                var retryPolicyConfig = clientConfig.HttpPolicyConfig.RetryPolicyConfig;
                if (retryPolicyConfig.IsUsed && retryPolicyConfig.RetryCount != null && retryPolicyConfig.RetryCount > 0)
                {
                    IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrResult(
                            msg => (retryPolicyConfig.AdditionalStatusCodes ?? Array.Empty<int>())
                                .Contains((int)msg.StatusCode))
                        .Or<SocketException>()
                        .OrInner<TimeoutRejectedException>()
                        .Or<TimeoutRejectedException>()
                        .WaitAndRetryAsync(
                            retryPolicyConfig.RetryCount.Value,
                            _ => TimeSpan.FromSeconds(retryPolicyConfig.RetryInterval ?? DefaultRetryInterval),
                            (_, _, context) => context.TryAdd(RetryKey, true));

                    var noOpPolicyHandler = Policy
                        .NoOpAsync()
                        .AsAsyncPolicy<HttpResponseMessage>();

                    httpBuilder = httpBuilder.AddPolicyHandler(
                        request =>
                        {
                            if (request.Method == HttpMethod.Get || retryAllHttpMethodsRequests)
                            {
                                return retryPolicy;
                            }

                            return noOpPolicyHandler;
                        })
                        .AddHttpMessageHandler<ApplicationInsightsPayloadTracerHandler>();
                }


                var timeoutPerTryPolicyConfig = clientConfig.HttpPolicyConfig.TimeoutPerTryPolicyConfig;
                if (timeoutPerTryPolicyConfig != null)
                {
                    if (timeoutPerTryPolicyConfig.IsUsed && timeoutPerTryPolicyConfig.TimeoutPerTry.TotalSeconds > 0)
                    {
                        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                            Convert.ToInt32(timeoutPerTryPolicyConfig.TimeoutPerTry.TotalSeconds));
                        // We place the timeoutPolicy inside the retryPolicy, to make it time out each try.
                        httpBuilder.AddPolicyHandler(timeoutPolicy);
                    }
                }

                var bulkheadPolicyConfig = clientConfig.HttpPolicyConfig.BulkheadPolicyConfig;
                if (bulkheadPolicyConfig != null)
                {
                    if (bulkheadPolicyConfig.IsUsed
                        && bulkheadPolicyConfig.MaxParallelization.HasValue
                        && bulkheadPolicyConfig.MaxParallelization > 0)
                    {
                        // Setup bulkhead policy. We use configured MaxParallelization value and int.MaxValue as maximum amount of
                        // queuing actions (to make sure Polly does not throw exceptions if a queue is too small)
                        var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(
                            bulkheadPolicyConfig.MaxParallelization.Value,
                            Int32.MaxValue);

                        httpBuilder.AddPolicyHandler(bulkheadPolicy);
                    }
                }

                var rateLimitPolicyConfig = clientConfig.HttpPolicyConfig.ThrottlingPolicyConfig;
                if (rateLimitPolicyConfig is { IsUsed: true, RequestsLimit: > 0 })
                {
                    httpBuilder.AddHttpMessageHandler(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<RequestThrottlingHandler>>();
                        return new RequestThrottlingHandler(
                            rateLimitPolicyConfig.RequestsLimit,
                            rateLimitPolicyConfig.Window,
                            rateLimitPolicyConfig.QueueLimit,
                            logger);
                    });
                }
            }

            return httpBuilder;
        }

        public class ApplicationInsightsPayloadTracerHandler : DelegatingHandler
        {
            private readonly TelemetryClient telemetryClient;

            public ApplicationInsightsPayloadTracerHandler(TelemetryClient telemetryClient)
            {
                this.telemetryClient = telemetryClient;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var isRetry = request.GetPolicyExecutionContext()?.ContainsKey(RetryKey) ?? false;
                if (!isRetry)
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                HttpResponseMessage response = null;
                var url = request.RequestUri;
                var telemetry = new DependencyTelemetry(
                    "Http",
                    url.Host,
                    url.AbsolutePath,
                    url.OriginalString);

                try
                {
                    telemetry.Start();

                    response = await base.SendAsync(request, cancellationToken);
                    var statusCode = (int)response.StatusCode;
                    telemetry.Success = statusCode < 400 && statusCode > 0;
                    telemetry.ResultCode = statusCode > 0
                        ? statusCode.ToString(CultureInfo.InvariantCulture)
                        : string.Empty;
                }
                catch
                {
                    telemetry.Success = false;
                    throw;
                }
                finally
                {
                    telemetry.Stop();
                    telemetryClient.TrackDependency(telemetry);
                }

                return response;
            }
        }
    }

    public class RequestThrottlingHandler : DelegatingHandler
    {
        private readonly FixedWindowRateLimiter rateLimiter;
        private readonly ILogger<RequestThrottlingHandler> logger;

        public RequestThrottlingHandler(
            int requestsLimit,
            TimeSpan windowDuration,
            int queueLimit,
            ILogger<RequestThrottlingHandler> logger)
        {
            this.logger = logger;

            FixedWindowRateLimiterOptions options = new()
            {
                PermitLimit = requestsLimit,
                Window = windowDuration,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit, // Queues up to 'queueLimit' requests; excess return TooManyRequests http code
                AutoReplenishment = true
            };
            rateLimiter = new FixedWindowRateLimiter(options);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var statisticsBefore = rateLimiter.GetStatistics();
            logger.LogInformation("RequestThrottling: attempt to get lease. Current queue length: {QueueCount}, available permits: {AvailablePermits}",
                statisticsBefore?.CurrentQueuedCount ?? 0,
                statisticsBefore?.CurrentAvailablePermits ?? 0);

            using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);
            if (lease.IsAcquired)
            {
                logger.LogInformation("RequestThrottling: lease received");
                return await base.SendAsync(request, cancellationToken);
            }

            logger.LogError("RequestThrottling: queueLimit is reached");
            TimeSpan? retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var ra) ? ra : null;
            HttpResponseMessage response = new(System.Net.HttpStatusCode.TooManyRequests);
            if (retryAfter.HasValue)
            {
                response.Headers.RetryAfter =
                    new System.Net.Http.Headers.RetryConditionHeaderValue(DateTimeOffset.UtcNow.Add(retryAfter.Value));
            }

            response.Headers.Add("X-RateLimit-Source", "Internal");
            response.Content = new StringContent("Request rejected by internal rate limiter.");
            response.ReasonPhrase = "Request rejected by internal rate limiter.";

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rateLimiter?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

}