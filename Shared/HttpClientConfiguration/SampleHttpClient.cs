using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// A sample class that produces a fully configured <see cref="HttpClient"/> based on
    /// <see cref="SampleHttpClientOptions"/>.
    ///
    /// <para>
    /// The HTTP behavior is not hard coded. Instead it is supplied as a
    /// <see cref="SampleHttpClientOptions"/> value through the
    /// <see cref="IOptions{TOptions}"/> pattern, which lets the settings be injected from the
    /// application configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>). Use
    /// <see cref="Create"/> to build a client directly from <c>App.config</c>, or construct one with an
    /// explicit <see cref="IOptions{TOptions}"/> when running inside a dependency injection container.
    /// </para>
    ///
    /// <para>
    /// Instantiate this class and inject its <see cref="Client"/> into any CyberSource SDK component
    /// that accepts an <see cref="HttpClient"/> (for example
    /// <c>IMerchantNetworkSettings.SetHttpClient(HttpClient)</c> on the client SDK). The single
    /// <see cref="HttpClient"/> instance is intended to be shared for the lifetime of the application.
    /// </para>
    ///
    /// <example>
    /// <code>
    /// // Build once from App.config and reuse for the lifetime of the application.
    /// using SampleHttpClient sample = SampleHttpClient.Create();
    ///
    /// // Inject into the SDK component(s).
    /// merchantNetworkSettings.SetHttpClient(sample.Client);
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <code>
    /// // Or bind the options through a dependency injection container.
    /// services.Configure&lt;SampleHttpClientOptions&gt;(_ =&gt; SampleHttpClientOptions.LoadFromAppConfig());
    /// services.AddSingleton&lt;SampleHttpClient&gt;();
    /// </code>
    /// </example>
    ///
    /// <remarks>
    /// This type owns both the underlying <see cref="HttpMessageHandler"/> and the produced
    /// <see cref="HttpClient"/> and disposes them when <see cref="Dispose"/> is called, so dispose it
    /// when the application shuts down.
    /// </remarks>
    public sealed class SampleHttpClient : IDisposable
    {
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleHttpClient"/> class using the HTTP client
        /// settings supplied through the <see cref="IOptions{TOptions}"/> pattern.
        /// </summary>
        /// <param name="options">
        /// The <see cref="SampleHttpClientOptions"/> that describe the timeout, headers, redirect,
        /// proxy and related HTTP behavior. These are typically bound from <c>App.config</c> via
        /// <see cref="SampleHttpClientOptions.LoadFromAppConfig"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        public SampleHttpClient(IOptions<SampleHttpClientOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var settings = options.Value;

            _handler = BuildHandler(settings);
            _client = BuildClient(_handler, settings);
        }

        /// <summary>
        /// The configured <see cref="HttpClient"/> instance. Inject this into SDK components that accept
        /// a shared <see cref="HttpClient"/>.
        /// </summary>
        public HttpClient Client => _client;

        /// <summary>
        /// Creates a <see cref="SampleHttpClient"/> whose settings are read from the application
        /// configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>).
        /// </summary>
        /// <returns>A ready to use <see cref="SampleHttpClient"/>.</returns>
        public static SampleHttpClient Create() =>
            new SampleHttpClient(Options.Create(SampleHttpClientOptions.LoadFromAppConfig()));

        /// <summary>
        /// Builds an <see cref="HttpClientHandler"/> from the supplied <see cref="SampleHttpClientOptions"/>.
        /// Exposed as <see langword="internal"/> so <see cref="SampleHttpClientFactory"/> can reuse it.
        /// </summary>
        internal static HttpClientHandler BuildHandler(SampleHttpClientOptions settings)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = settings.AllowAutoRedirect,
                MaxAutomaticRedirections = Math.Max(1, settings.MaxAutomaticRedirections),
                AutomaticDecompression = settings.AutomaticDecompression,
                UseCookies = settings.UseCookies,
                PreAuthenticate = settings.PreAuthenticate,
                UseProxy = settings.UseProxy,
                MaxConnectionsPerServer = Math.Max(1, settings.MaxConnectionsPerServer),
            };

            if (settings.UseProxy && !string.IsNullOrWhiteSpace(settings.ProxyAddress))
            {
                handler.Proxy = new WebProxy(settings.ProxyAddress, settings.ProxyBypassOnLocal);
            }

            return handler;
        }

        /// <summary>
        /// Builds an <see cref="HttpClient"/> on top of the supplied handler using the supplied
        /// <see cref="SampleHttpClientOptions"/>. Exposed as <see langword="internal"/> so
        /// <see cref="SampleHttpClientFactory"/> can reuse it.
        /// </summary>
        internal static HttpClient BuildClient(HttpMessageHandler handler, SampleHttpClientOptions settings)
        {
            var client = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = settings.TimeoutSeconds > 0
                    ? TimeSpan.FromSeconds(settings.TimeoutSeconds)
                    : Timeout.InfiniteTimeSpan,
                MaxResponseContentBufferSize = settings.MaxResponseContentBufferSizeBytes > 0
                    ? settings.MaxResponseContentBufferSizeBytes
                    : int.MaxValue,
            };

            if (!string.IsNullOrWhiteSpace(settings.BaseAddress))
            {
                client.BaseAddress = new Uri(settings.BaseAddress, UriKind.Absolute);
            }

            if (!string.IsNullOrWhiteSpace(settings.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
            }

            return client;
        }

        /// <summary>
        /// Disposes the underlying <see cref="HttpClient"/> and its <see cref="HttpClientHandler"/>.
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
            _handler.Dispose();
        }
    }
}
