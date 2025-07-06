namespace Microsoft.Extensions.Hosting;

// Extension method to apply common defaults to project resources
public static class ResourceBuilderExtensions
{
    public static IResourceBuilder<ProjectResource> WithServiceDefaults(this IResourceBuilder<ProjectResource> builder)
    {
        return builder
            .WithEnvironment("Logging__Console__FormatterName", "systemd")
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");
    }
}