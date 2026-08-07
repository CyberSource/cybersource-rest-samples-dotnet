using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using CyberSource.Client;
using CyberSource.Utilities.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cybersource_rest_samples_dotnet
{
    /// <summary>
    /// Sample bootstrapper that demonstrates how to feed caller-owned JSON options into the
    /// CyberSource REST SDK through its <see cref="IOptionsMonitor{TOptions}"/>-based serialization
    /// dependency-injection surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SDK exposes wrapper types - <see cref="SdkSerializerOptions"/> (request
    /// serialization) and <see cref="SdkDeserializerOptions"/> (response deserialization,
    /// including the Payment-Links model track derived from the same monitor) - each with its
    /// own <see cref="IPostConfigureOptions{TOptions}"/> that carries the SDK's mandatory
    /// invariants. Registering the pipeline with
    /// <see cref="ServiceCollectionExtensions.AddSerialization(IServiceCollection)"/>
    /// makes the monitors resolvable from the DI container. The
    /// <see cref="CyberSource.Client.Configuration"/> constructor then accepts those monitors
    /// directly through its <c>serializerOptionsMonitor</c> and <c>deserializerOptionsMonitor</c>
    /// parameters, and <see cref="ApiClient"/> reads
    /// <c>CurrentValue.Options</c> from each at construction time.
    /// </para>
    /// <para>
    /// This bootstrapper layers sample <see cref="PostConfigureOptions{TOptions}"/> callbacks onto
    /// each wrapper. These callbacks run BEFORE the SDK's own post-configures, so the SDK invariants
    /// (<c>EnforceExtraFieldConflicts</c> resolver,
    /// <see cref="JsonIgnoreCondition.WhenWritingNull"/>, required CyberSource model converters)
    /// always win where they conflict - consumer settings are additive value-add.
    /// </para>
    /// </remarks>
    public static class SampleSerializerOptions
    {
        /// <summary>
        /// Builds a service provider that has the SDK serialization pipeline registered and the
        /// sample post-configures layered on top.
        /// </summary>
        /// <returns>
        /// A fully constructed <see cref="ServiceProvider"/> from which the three
        /// <see cref="IOptionsMonitor{TOptions}"/> instances can be resolved.
        /// </returns>
        public static ServiceProvider BuildSampleServiceProvider()
        {
            var services = new ServiceCollection();

            // Register the SDK-owned JSON options pipeline. This wires:
            //   - IOptionsMonitor<SdkSerializerOptions>
            //   - IOptionsMonitor<SdkDeserializerOptions>
            // and appends the SDK's mandatory IPostConfigureOptions<T> for each track. Any
            // caller-registered PostConfigure<T> callbacks run BEFORE those, so SDK invariants
            // are always applied on top.
            services.AddSerialization();

            // -----------------------------------------------------------------------------------
            // Sample post-configure for request serialization (SdkSerializerOptions).
            // -----------------------------------------------------------------------------------
            // The SDK's SdkSerializerOptions.Options is seeded with JsonSerializerDefaults.Web +
            // DefaultIgnoreCondition = WhenWritingNull. We add caller value-add on top:
            //   - UnsafeRelaxedJsonEscaping so localized characters (e.g. billing names) are not
            //     over-escaped in the request body.
            //   - Compact wire payloads (WriteIndented = false).
            //   - Strict JsonStringEnumConverter (allowIntegerValues: false) so consumer-owned
            //     enum-typed extra fields serialize as their string names.
            services.PostConfigure<SdkSerializerOptions>(wrapper =>
            {
                var opts = wrapper.Options;
                opts.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                opts.WriteIndented = false;
                opts.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            });

            // -----------------------------------------------------------------------------------
            // Sample post-configure for general response deserialization (SdkDeserializerOptions).
            // -----------------------------------------------------------------------------------
            // The SDK's SdkDeserializerOptions.Options is seeded with JsonSerializerDefaults.Web.
            // We add caller value-add on top:
            //   - PropertyNameCaseInsensitive so responses with inconsistent field casing bind.
            //   - NumberHandling.AllowReadingFromString so numeric-looking JSON strings bind to
            //     numeric properties (CyberSource occasionally returns amounts / codes as strings).
            //   - AllowTrailingCommas + ReadCommentHandling.Skip for resilience against upstream
            //     proxies that may inject either.
            //   - Lenient JsonStringEnumConverter for consumer-owned enum-typed extra fields.
            services.PostConfigure<SdkDeserializerOptions>(wrapper =>
            {
                var opts = wrapper.Options;
                opts.PropertyNameCaseInsensitive = true;
                opts.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                opts.AllowTrailingCommas = true;
                opts.ReadCommentHandling = JsonCommentHandling.Skip;
                opts.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Builds a sample <see cref="JsonSerializerOptions"/> instance for request-payload
        /// serialization, suitable for handing to
        /// <see cref="CyberSource.Client.Configuration"/> via its <c>serializationOptions</c>
        /// constructor parameter (direct-injection path, no DI container required).
        /// </summary>
        /// <remarks>
        /// The returned instance carries the same caller value-add tuning as the sample
        /// <see cref="SdkSerializerOptions"/> post-configure registered by
        /// <see cref="BuildSampleServiceProvider"/>. The SDK's <see cref="ApiClient"/> constructor
        /// merges this instance with its mandatory invariants (
        /// <c>EnforceExtraFieldConflicts</c> resolver, <see cref="JsonIgnoreCondition.WhenWritingNull"/>,
        /// required converters) so any caller setting that conflicts with an SDK invariant is
        /// overridden by the SDK.
        /// </remarks>
        /// <returns>A fresh <see cref="JsonSerializerOptions"/> ready to hand to the SDK.</returns>
        public static JsonSerializerOptions CreateSerializationOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            ApplySampleSerializerTuning(options);
            return options;
        }

        /// <summary>
        /// Builds a sample <see cref="JsonSerializerOptions"/> instance for response-payload
        /// deserialization, suitable for handing to <see cref="CyberSource.Client.Configuration"/>
        /// via its <c>deserializationOptions</c> constructor parameter (direct-injection path, no
        /// DI container required).
        /// </summary>
        /// <remarks>
        /// The returned instance carries the same caller value-add tuning as the sample
        /// <see cref="SdkDeserializerOptions"/> post-configure registered by
        /// <see cref="BuildSampleServiceProvider"/>. The SDK's <see cref="ApiClient"/> constructor
        /// layers the required CyberSource model converters on top, so consumers do not need to
        /// add them here.
        /// </remarks>
        /// <returns>A fresh <see cref="JsonSerializerOptions"/> ready to hand to the SDK.</returns>
        public static JsonSerializerOptions CreateDeserializationOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            ApplySampleDeserializerTuning(options);
            return options;
        }

        private static void ApplySampleSerializerTuning(JsonSerializerOptions options)
        {
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.WriteIndented = false;
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        }

        private static void ApplySampleDeserializerTuning(JsonSerializerOptions options)
        {
            options.PropertyNameCaseInsensitive = true;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            options.AllowTrailingCommas = true;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
        }
    }
}
