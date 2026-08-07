using System;
using System.Collections.Specialized;
using System.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// The console log formatter used by <see cref="ConsoleLoggerFactory"/>.
    /// </summary>
    public enum ConsoleFormatterKind
    {
        /// <summary>Human readable single/multi line output (the built-in "simple" formatter).</summary>
        Simple,

        /// <summary>Output shaped for the systemd journal (no colors, single line).</summary>
        Systemd,

        /// <summary>Structured JSON output, one JSON document per log entry.</summary>
        Json
    }

    /// <summary>
    /// Strongly typed logging settings that are bound from the application configuration file
    /// (<c>App.config</c> / <c>&lt;app&gt;.exe.config</c>) and consumed by <see cref="ConsoleLoggerFactory"/>
    /// through the <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> pattern.
    ///
    /// <para>
    /// Each property maps to an <c>&lt;appSettings&gt;</c> entry whose <c>key</c> is the property name
    /// prefixed with <see cref="AppSettingsPrefix"/>, for example
    /// <c>&lt;add key="Logging:Console:MinimumLogLevel" value="Information" /&gt;</c>. Any entry that is
    /// missing (or blank) simply keeps the default value declared on the property.
    /// </para>
    /// </summary>
    public sealed class ConsoleLoggingOptions
    {
        /// <summary>
        /// The prefix applied to every <c>&lt;appSettings&gt;</c> key that feeds these options.
        /// </summary>
        public const string AppSettingsPrefix = "Logging:Console:";

        /// <summary>
        /// The minimum <see cref="LogLevel"/> to emit. Entries below this level are suppressed.
        /// Defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Selects which console formatter is used. Defaults to <see cref="ConsoleFormatterKind.Simple"/>.
        /// </summary>
        public ConsoleFormatterKind FormatterName { get; set; } = ConsoleFormatterKind.Simple;

        /// <summary>
        /// When <see langword="true"/>, logging scopes are included in the output. Defaults to <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// A <see cref="DateTime"/> format string used to prefix each entry with a timestamp
        /// (for example <c>"yyyy-MM-dd HH:mm:ss "</c>). When <see langword="null"/> or empty no timestamp is written.
        /// </summary>
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// When <see langword="true"/>, timestamps use UTC instead of local time. Defaults to <see langword="false"/>.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }

        /// <summary>
        /// When <see langword="true"/>, the simple formatter writes each entry on a single line.
        /// Only honored by <see cref="ConsoleFormatterKind.Simple"/>. Defaults to <see langword="false"/>.
        /// </summary>
        public bool SingleLine { get; set; }

        /// <summary>
        /// Controls ANSI color usage for the simple formatter. Defaults to <see cref="LoggerColorBehavior.Default"/>.
        /// </summary>
        public LoggerColorBehavior ColorBehavior { get; set; } = LoggerColorBehavior.Default;

        /// <summary>
        /// When set, entries at this level and above are written to standard error instead of standard output.
        /// When <see langword="null"/> everything is written to standard output.
        /// </summary>
        public LogLevel? LogToStandardErrorThreshold { get; set; }

        /// <summary>
        /// When <see langword="true"/>, JSON output is pretty printed. Only honored by
        /// <see cref="ConsoleFormatterKind.Json"/>. Defaults to <see langword="false"/>.
        /// </summary>
        public bool JsonIndented { get; set; }

        /// <summary>
        /// Reads the <c>&lt;appSettings&gt;</c> section of the application configuration file and binds the
        /// recognized <see cref="AppSettingsPrefix"/> keys into a new <see cref="ConsoleLoggingOptions"/> instance.
        /// Unrecognized or blank keys leave the corresponding property at its default value.
        /// </summary>
        /// <returns>A populated <see cref="ConsoleLoggingOptions"/> instance.</returns>
        public static ConsoleLoggingOptions LoadFromAppConfig()
        {
            var settings = ConfigurationManager.AppSettings;
            var options = new ConsoleLoggingOptions();

            if (TryGetEnum(settings, nameof(MinimumLogLevel), out LogLevel minimumLogLevel))
            {
                options.MinimumLogLevel = minimumLogLevel;
            }

            if (TryGetEnum(settings, nameof(FormatterName), out ConsoleFormatterKind formatter))
            {
                options.FormatterName = formatter;
            }

            if (TryGetBool(settings, nameof(IncludeScopes), out bool includeScopes))
            {
                options.IncludeScopes = includeScopes;
            }

            var timestampFormat = settings[AppSettingsPrefix + nameof(TimestampFormat)];
            if (!string.IsNullOrEmpty(timestampFormat))
            {
                options.TimestampFormat = timestampFormat;
            }

            if (TryGetBool(settings, nameof(UseUtcTimestamp), out bool useUtcTimestamp))
            {
                options.UseUtcTimestamp = useUtcTimestamp;
            }

            if (TryGetBool(settings, nameof(SingleLine), out bool singleLine))
            {
                options.SingleLine = singleLine;
            }

            if (TryGetEnum(settings, nameof(ColorBehavior), out LoggerColorBehavior colorBehavior))
            {
                options.ColorBehavior = colorBehavior;
            }

            if (TryGetEnum(settings, nameof(LogToStandardErrorThreshold), out LogLevel errorThreshold))
            {
                options.LogToStandardErrorThreshold = errorThreshold;
            }

            if (TryGetBool(settings, nameof(JsonIndented), out bool jsonIndented))
            {
                options.JsonIndented = jsonIndented;
            }

            return options;
        }

        private static bool TryGetBool(NameValueCollection settings, string name, out bool value)
        {
            var raw = settings[AppSettingsPrefix + name];
            return bool.TryParse(raw, out value);
        }

        private static bool TryGetEnum<TEnum>(NameValueCollection settings, string name, out TEnum value)
            where TEnum : struct
        {
            var raw = settings[AppSettingsPrefix + name];
            return Enum.TryParse(raw, ignoreCase: true, out value) && Enum.IsDefined(typeof(TEnum), value);
        }
    }
}
