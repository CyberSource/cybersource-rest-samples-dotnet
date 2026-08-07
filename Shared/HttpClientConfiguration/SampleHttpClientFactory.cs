using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// A sample <see cref="IHttpClientFactory"/> implementation that hands out <see cref="HttpClient"/>
    /// instances configured from <see cref="SampleHttpClientOptions"/>.
    ///
    /// <para>
    /// The HTTP behavior is not hard coded. Instead it is supplied as a
    /// <see cref="SampleHttpClientOptions"/> value through the
    /// <see cref="IOptions{TOptions}"/> pattern, which lets the settings be injected from the
    /// application configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>). Use
    /// <see cref="Create"/> to build a factory directly from <c>App.config</c>, or construct one with an
    /// explicit <see cref="IOptions{TOptions}"/> when running inside a dependency injection container.
    /// </para>
    ///
    /// <para>
    /// Instantiate this class and inject it into any CyberSource SDK component that accepts an
    /// <see cref="IHttpClientFactory"/> (for example
    /// <c>IMerchantNetworkSettings.SetHttpClientFactory(IHttpClientFactory)</c> on the client SDK).
    /// Every call to <see cref="CreateClient(string)"/> with the same name returns the same cached
    /// <see cref="HttpClient"/> until the handler lifetime expires, at which point the factory
    /// transparently rotates to a fresh handler so DNS changes are picked up.
    /// </para>
    ///
    /// <example>
    /// <code>
    /// // Build once from App.config and reuse for the lifetime of the application.
    /// using SampleHttpClientFactory factory = SampleHttpClientFactory.Create();
    ///
    /// // Inject into the SDK component(s).
    /// merchantNetworkSettings.SetHttpClientFactory(factory, clientName: "cybersource");
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <code>
    /// // Or bind the options through a dependency injection container.
    /// services.Configure&lt;SampleHttpClientOptions&gt;(_ =&gt; SampleHttpClientOptions.LoadFromAppConfig());
    /// services.AddSingleton&lt;IHttpClientFactory, SampleHttpClientFactory&gt;();
    /// </code>
    /// </example>
    ///
    /// <remarks>
    /// This is a minimal educational implementation. It caches one <see cref="HttpClient"/> per name and
    /// rotates it after <see cref="SampleHttpClientOptions.HandlerLifetimeMinutes"/> so long lived
    /// clients continue to see DNS updates. For production workloads prefer the built-in factory from
    /// <c>Microsoft.Extensions.Http</c>, which additionally pools message handlers, tracks per-handler
    /// reference counts and integrates with <c>IServiceCollection.AddHttpClient</c>.
    /// </remarks>
    public sealed class SampleHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly SampleHttpClientOptions _options;
        private readonly TimeSpan _handlerLifetime;
        private readonly ConcurrentDictionary<string, CachedClient> _clients =
            new ConcurrentDictionary<string, CachedClient>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleHttpClientFactory"/> class using the HTTP
        /// client settings supplied through the <see cref="IOptions{TOptions}"/> pattern.
        /// </summary>
        /// <param name="options">
        /// The <see cref="SampleHttpClientOptions"/> that describe the timeout, headers, redirect,
        /// proxy, handler lifetime and related HTTP behavior. These are typically bound from
        /// <c>App.config</c> via <see cref="SampleHttpClientOptions.LoadFromAppConfig"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        public SampleHttpClientFactory(IOptions<SampleHttpClientOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
            _handlerLifetime = _options.HandlerLifetimeMinutes > 0
                ? TimeSpan.FromMinutes(_options.HandlerLifetimeMinutes)
                : TimeSpan.FromMinutes(2);
        }

        /// <summary>
        /// Creates a <see cref="SampleHttpClientFactory"/> whose settings are read from the application
        /// configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>).
        /// </summary>
        /// <returns>A ready to use <see cref="SampleHttpClientFactory"/>.</returns>
        public static SampleHttpClientFactory Create() =>
            new SampleHttpClientFactory(Options.Create(SampleHttpClientOptions.LoadFromAppConfig()));

        /// <summary>
        /// Returns an <see cref="HttpClient"/> for the given logical name. Clients are cached per name
        /// and transparently rotated when the handler lifetime elapses so DNS changes are picked up.
        /// </summary>
        /// <param name="name">
        /// A logical name for the client. Different names may be configured differently in a real
        /// factory; this sample applies the same <see cref="SampleHttpClientOptions"/> to every name and
        /// caches the resulting client per name.
        /// </param>
        /// <returns>A shared <see cref="HttpClient"/> instance for <paramref name="name"/>.</returns>
        public HttpClient CreateClient(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            while (true)
            {
                var cached = _clients.GetOrAdd(name, _ => BuildCached());
                if (!cached.IsExpired(_handlerLifetime))
                {
                    return cached.Client;
                }

                if (_clients.TryUpdate(name, BuildCached(), cached))
                {
                    cached.Dispose();
                }
            }
        }

        private CachedClient BuildCached()
        {
            var handler = SampleHttpClient.BuildHandler(_options);
            var client = SampleHttpClient.BuildClient(handler, _options);
            return new CachedClient(client, handler, DateTime.UtcNow);
        }

        /// <summary>
        /// Disposes every cached <see cref="HttpClient"/> and its underlying handler.
        /// </summary>
        public void Dispose()
        {
            foreach (var cached in _clients.Values)
            {
                cached.Dispose();
            }

            _clients.Clear();
        }

        private sealed class CachedClient : IDisposable
        {
            private readonly HttpClientHandler _handler;
            private readonly DateTime _createdUtc;

            public CachedClient(HttpClient client, HttpClientHandler handler, DateTime createdUtc)
            {
                Client = client;
                _handler = handler;
                _createdUtc = createdUtc;
            }

            public HttpClient Client { get; }

            public bool IsExpired(TimeSpan handlerLifetime) =>
                DateTime.UtcNow - _createdUtc >= handlerLifetime;

            public void Dispose()
            {
                Client.Dispose();
                _handler.Dispose();
            }
        }
    }
}
