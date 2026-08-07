using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// A sample <see cref="ILoggerFactory"/> implementation backed by the built-in console logging
    /// provider (the <c>Microsoft.Extensions.Logging.Console</c> package).
    ///
    /// <para>
    /// The console behavior is not hard coded. Instead it is supplied as a
    /// <see cref="ConsoleLoggingOptions"/> value through the
    /// <see cref="IOptions{TOptions}"/> pattern, which lets the settings be injected from the
    /// application configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>). Use
    /// <see cref="Create"/> to build a factory directly from <c>App.config</c>, or construct one with an
    /// explicit <see cref="IOptions{TOptions}"/> when running inside a dependency injection container.
    /// </para>
    ///
    /// <para>
    /// Instantiate this class and inject it into any CyberSource SDK component that accepts an
    /// <see cref="ILoggerFactory"/> (for example the client <c>Configuration</c>, the authentication
    /// <c>MerchantConfig</c>, or the various merchant settings classes). All SDK log output is then
    /// written to the console.
    /// </para>
    ///
    /// <example>
    /// <code>
    /// // Build once from App.config and reuse for the lifetime of the application.
    /// ILoggerFactory loggerFactory = ConsoleLoggerFactory.Create();
    ///
    /// // Inject into the SDK component(s).
    /// var merchantConfig = new MerchantConfig(configurationDictionary, loggerFactory: loggerFactory);
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <code>
    /// // Or bind the options through a dependency injection container.
    /// services.Configure&lt;ConsoleLoggingOptions&gt;(_ =&gt; ConsoleLoggingOptions.LoadFromAppConfig());
    /// services.AddSingleton&lt;ILoggerFactory, ConsoleLoggerFactory&gt;();
    /// </code>
    /// </example>
    ///
    /// <remarks>
    /// This type is a thin decorator over the factory produced by
    /// <see cref="LoggerFactory.Create(Action{ILoggingBuilder})"/>. It owns that inner factory and
    /// disposes it when <see cref="Dispose"/> is called, so dispose it when the application shuts down.
    /// </remarks>
    public sealed class ConsoleLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _innerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLoggerFactory"/> class using the console
        /// logging settings supplied through the <see cref="IOptions{TOptions}"/> pattern.
        /// </summary>
        /// <param name="options">
        /// The <see cref="ConsoleLoggingOptions"/> that describe the minimum level, formatter and related
        /// console behavior. These are typically bound from <c>App.config</c> via
        /// <see cref="ConsoleLoggingOptions.LoadFromAppConfig"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        public ConsoleLoggerFactory(IOptions<ConsoleLoggingOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var settings = options.Value;

            _innerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(settings.MinimumLogLevel);

                switch (settings.FormatterName)
                {
                    case ConsoleFormatterKind.Json:
                        builder.AddJsonConsole(formatter =>
                        {
                            formatter.IncludeScopes = settings.IncludeScopes;
                            formatter.TimestampFormat = settings.TimestampFormat;
                            formatter.UseUtcTimestamp = settings.UseUtcTimestamp;
                            formatter.JsonWriterOptions = new JsonWriterOptions { Indented = settings.JsonIndented };
                        });
                        break;

                    case ConsoleFormatterKind.Systemd:
                        builder.AddSystemdConsole(formatter =>
                        {
                            formatter.IncludeScopes = settings.IncludeScopes;
                            formatter.TimestampFormat = settings.TimestampFormat;
                            formatter.UseUtcTimestamp = settings.UseUtcTimestamp;
                        });
                        break;

                    default:
                        builder.AddSimpleConsole(formatter =>
                        {
                            formatter.IncludeScopes = settings.IncludeScopes;
                            formatter.TimestampFormat = settings.TimestampFormat;
                            formatter.UseUtcTimestamp = settings.UseUtcTimestamp;
                            formatter.SingleLine = settings.SingleLine;
                            formatter.ColorBehavior = settings.ColorBehavior;
                        });
                        break;
                }

                if (settings.LogToStandardErrorThreshold.HasValue)
                {
                    builder.AddConsole(console =>
                        console.LogToStandardErrorThreshold = settings.LogToStandardErrorThreshold.Value);
                }
            });
        }

        /// <summary>
        /// Creates a <see cref="ConsoleLoggerFactory"/> whose settings are read from the application
        /// configuration file (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>).
        /// </summary>
        /// <returns>A ready to use <see cref="ConsoleLoggerFactory"/>.</returns>
        public static ConsoleLoggerFactory Create() =>
            new ConsoleLoggerFactory(Options.Create(ConsoleLoggingOptions.LoadFromAppConfig()));

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance for the given category name.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>An <see cref="ILogger"/> that writes to the console.</returns>
        public ILogger CreateLogger(string categoryName) => _innerFactory.CreateLogger(categoryName);

        /// <summary>
        /// Adds an <see cref="ILoggerProvider"/> to the underlying factory.
        /// </summary>
        /// <param name="provider">The logging provider to add.</param>
        public void AddProvider(ILoggerProvider provider) => _innerFactory.AddProvider(provider);

        /// <summary>
        /// Disposes the underlying logger factory and flushes any buffered console output.
        /// </summary>
        public void Dispose() => _innerFactory.Dispose();
    }
}
