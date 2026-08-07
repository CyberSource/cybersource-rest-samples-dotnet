using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// Strongly typed HTTP client settings that are bound from the application configuration file
    /// (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>) and consumed by <see cref="SampleHttpClient"/>
    /// and <see cref="SampleHttpClientFactory"/> through the
    /// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> pattern.
    ///
    /// <para>
    /// Each property maps to an <c>&lt;appSettings&gt;</c> entry whose <c>key</c> is the property name
    /// prefixed with <see cref="AppSettingsPrefix"/>, for example
    /// <c>&lt;add key="HttpClient:TimeoutSeconds" value="30" /&gt;</c>. Any entry that is missing (or
    /// blank) simply keeps the default value declared on the property.
    /// </para>
    /// </summary>
    public sealed class SampleHttpClientOptions
    {
        /// <summary>
        /// The prefix applied to every <c>&lt;appSettings&gt;</c> key that feeds these options.
        /// </summary>
        public const string AppSettingsPrefix = "HttpClient:";

        // ------------------------------------------------------------------- HttpClient-level

        /// <summary>
        /// Optional base address applied to every request that uses a relative URI. When
        /// <see langword="null"/> the caller must always pass an absolute URI. Defaults to <see langword="null"/>.
        /// </summary>
        public string? BaseAddress { get; set; }

        /// <summary>
        /// The overall request timeout, in seconds. Applies to the whole request/response including
        /// content download. Set to a value &lt;= 0 to use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
        /// Defaults to 100 seconds (the <see cref="System.Net.Http.HttpClient"/> default).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 100;

        /// <summary>
        /// The <c>User-Agent</c> header sent on every request. Defaults to <c>"CyberSourceRestSamples/1.0"</c>.
        /// </summary>
        public string UserAgent { get; set; } = "CyberSourceRestSamples/1.0";

        /// <summary>
        /// The maximum number of bytes buffered when reading a response. Defaults to
        /// <c>int.MaxValue</c> (approximately 2&#160;GB, matching the <see cref="System.Net.Http.HttpClient"/> default).
        /// </summary>
        public long MaxResponseContentBufferSizeBytes { get; set; } = int.MaxValue;

        // ------------------------------------------------------------------- Handler-level

        /// <summary>
        /// When <see langword="true"/> the handler follows HTTP redirect responses automatically.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool AllowAutoRedirect { get; set; } = true;

        /// <summary>
        /// The maximum number of redirects the handler will follow before giving up.
        /// Only honored when <see cref="AllowAutoRedirect"/> is <see langword="true"/>. Defaults to 50.
        /// </summary>
        public int MaxAutomaticRedirections { get; set; } = 50;

        /// <summary>
        /// The set of automatic decompression methods requested from the server (via <c>Accept-Encoding</c>).
        /// Common values on .NET Framework: <c>None</c>, <c>GZip</c>, <c>Deflate</c>, or <c>"GZip, Deflate"</c>.
        /// Defaults to <see cref="DecompressionMethods.None"/>.
        /// </summary>
        public DecompressionMethods AutomaticDecompression { get; set; } = DecompressionMethods.None;

        /// <summary>
        /// When <see langword="true"/> the handler stores and resends cookies across requests.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool UseCookies { get; set; } = true;

        /// <summary>
        /// When <see langword="true"/> the handler sends credentials preemptively on the first request
        /// rather than waiting for a <c>401 Unauthorized</c> challenge. Defaults to <see langword="false"/>.
        /// </summary>
        public bool PreAuthenticate { get; set; }

        /// <summary>
        /// When <see langword="true"/> the handler uses the system or configured proxy. Set to
        /// <see langword="false"/> to bypass all proxies. Defaults to <see langword="true"/>.
        /// </summary>
        public bool UseProxy { get; set; } = true;

        /// <summary>
        /// Optional proxy URI (for example <c>"http://proxy.corp.example.com:8080"</c>). Only honored when
        /// <see cref="UseProxy"/> is <see langword="true"/>. When <see langword="null"/> the system proxy is used.
        /// Defaults to <see langword="null"/>.
        /// </summary>
        public string? ProxyAddress { get; set; }

        /// <summary>
        /// When <see langword="true"/> requests to local (intranet) hosts bypass the proxy defined by
        /// <see cref="ProxyAddress"/>. Defaults to <see langword="true"/>.
        /// </summary>
        public bool ProxyBypassOnLocal { get; set; } = true;

        /// <summary>
        /// The maximum number of concurrent TCP connections allowed per remote server. Defaults to
        /// <see cref="int.MaxValue"/>.
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = int.MaxValue;

        // ------------------------------------------------------------------- Factory-level

        /// <summary>
        /// The lifetime, in minutes, of a handler pooled by <see cref="SampleHttpClientFactory"/>.
        /// After this interval the factory rotates handlers so DNS changes are picked up. Defaults to 2 minutes
        /// (matching the standard <c>Microsoft.Extensions.Http</c> default).
        /// </summary>
        public int HandlerLifetimeMinutes { get; set; } = 2;

        /// <summary>
        /// Reads the <c>&lt;appSettings&gt;</c> section of the application configuration file and binds the
        /// recognized <see cref="AppSettingsPrefix"/> keys into a new <see cref="SampleHttpClientOptions"/> instance.
        /// Unrecognized or blank keys leave the corresponding property at its default value.
        /// </summary>
        /// <returns>A populated <see cref="SampleHttpClientOptions"/> instance.</returns>
        public static SampleHttpClientOptions LoadFromAppConfig()
        {
            var settings = ConfigurationManager.AppSettings;
            var options = new SampleHttpClientOptions();

            var baseAddress = settings[AppSettingsPrefix + nameof(BaseAddress)];
            if (!string.IsNullOrWhiteSpace(baseAddress))
            {
                options.BaseAddress = baseAddress;
            }

            if (TryGetInt(settings, nameof(TimeoutSeconds), out int timeoutSeconds))
            {
                options.TimeoutSeconds = timeoutSeconds;
            }

            var userAgent = settings[AppSettingsPrefix + nameof(UserAgent)];
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                options.UserAgent = userAgent;
            }

            if (TryGetLong(settings, nameof(MaxResponseContentBufferSizeBytes), out long maxBuffer))
            {
                options.MaxResponseContentBufferSizeBytes = maxBuffer;
            }

            if (TryGetBool(settings, nameof(AllowAutoRedirect), out bool allowAutoRedirect))
            {
                options.AllowAutoRedirect = allowAutoRedirect;
            }

            if (TryGetInt(settings, nameof(MaxAutomaticRedirections), out int maxRedirects))
            {
                options.MaxAutomaticRedirections = maxRedirects;
            }

            if (TryGetEnum(settings, nameof(AutomaticDecompression), out DecompressionMethods decompression))
            {
                options.AutomaticDecompression = decompression;
            }

            if (TryGetBool(settings, nameof(UseCookies), out bool useCookies))
            {
                options.UseCookies = useCookies;
            }

            if (TryGetBool(settings, nameof(PreAuthenticate), out bool preAuthenticate))
            {
                options.PreAuthenticate = preAuthenticate;
            }

            if (TryGetBool(settings, nameof(UseProxy), out bool useProxy))
            {
                options.UseProxy = useProxy;
            }

            var proxyAddress = settings[AppSettingsPrefix + nameof(ProxyAddress)];
            if (!string.IsNullOrWhiteSpace(proxyAddress))
            {
                options.ProxyAddress = proxyAddress;
            }

            if (TryGetBool(settings, nameof(ProxyBypassOnLocal), out bool proxyBypass))
            {
                options.ProxyBypassOnLocal = proxyBypass;
            }

            if (TryGetInt(settings, nameof(MaxConnectionsPerServer), out int maxConnections))
            {
                options.MaxConnectionsPerServer = maxConnections;
            }

            if (TryGetInt(settings, nameof(HandlerLifetimeMinutes), out int handlerLifetime))
            {
                options.HandlerLifetimeMinutes = handlerLifetime;
            }

            return options;
        }

        private static bool TryGetBool(NameValueCollection settings, string name, out bool value)
        {
            var raw = settings[AppSettingsPrefix + name];
            return bool.TryParse(raw, out value);
        }

        private static bool TryGetInt(NameValueCollection settings, string name, out int value)
        {
            var raw = settings[AppSettingsPrefix + name];
            return int.TryParse(raw, out value);
        }

        private static bool TryGetLong(NameValueCollection settings, string name, out long value)
        {
            var raw = settings[AppSettingsPrefix + name];
            return long.TryParse(raw, out value);
        }

        private static bool TryGetEnum<TEnum>(NameValueCollection settings, string name, out TEnum value)
            where TEnum : struct
        {
            var raw = settings[AppSettingsPrefix + name];
            return Enum.TryParse(raw, ignoreCase: true, out value);
        }
    }
}
