namespace Retention.IntegrationTests;

/// <summary>
/// Collection definition to ensure TelemetryTests run serially.
/// This prevents ActivityListener from capturing activities from parallel tests.
/// </summary>
[CollectionDefinition("Telemetry", DisableParallelization = true)]
public class TelemetryTestCollection
{
}
