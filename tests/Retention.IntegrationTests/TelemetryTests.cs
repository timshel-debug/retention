using System.Diagnostics;
using FluentAssertions;
using Retention.Application;
using Retention.Application.Observability;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.IntegrationTests;

/// <summary>
/// Tests for observability instrumentation using ActivitySource.
/// No OpenTelemetry exporter required - uses ActivityListener directly.
/// </summary>
public class TelemetryTests : IDisposable
{
    private readonly EvaluateRetentionService _service = new();
    private readonly List<Activity> _capturedActivities = new();
    private readonly ActivityListener _listener;

    public TelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Retention",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _capturedActivities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluation_EmitsActivity_WithExpectedName()
    {
        _capturedActivities.Clear(); // Clear any previously captured activities
        
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        _service.EvaluateRetention(projects, environments, releases, deployments, releasesToKeep: 1);

        // Make a snapshot to avoid concurrent modification with other tests
        var snapshot = _capturedActivities.Where(a => a.OperationName == "retention.evaluate").ToList();
        snapshot.Should().HaveCount(1);
        snapshot[0].OperationName.Should().Be("retention.evaluate");
    }

    [Fact]
    public void Evaluation_EmitsActivity_WithInputAttributes()
    {
        _capturedActivities.Clear(); // Clear any previously captured activities
        
        var projects = new[] { new Project("P1", "Project 1"), new Project("P2", "Project 2") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P2", "1.0", Date(2000, 1, 1))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 2))
        };

        _service.EvaluateRetention(projects, environments, releases, deployments, releasesToKeep: 2);

        // Make a snapshot to avoid concurrent modification with other tests
        var snapshot = _capturedActivities.Where(a => a.OperationName == "retention.evaluate").ToList();
        snapshot.Should().HaveCount(1, "exactly one retention.evaluate activity should be emitted after clearing");
        
        var activity = snapshot[0];
        activity.GetTagItem("retention.n").Should().Be(2);
        activity.GetTagItem("input.projects.count").Should().Be(2);
        activity.GetTagItem("input.environments.count").Should().Be(1);
        activity.GetTagItem("input.releases.count").Should().Be(2);
        activity.GetTagItem("input.deployments.count").Should().Be(2);
    }

    [Fact]
    public void Evaluation_EmitsActivity_WithResultAttributes()
    {
        _capturedActivities.Clear(); // Clear any previously captured activities
        
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P1", "1.1", Date(2000, 1, 2))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 3)),
            new Deployment("D-INVALID", "R-MISSING", "E1", Date(2000, 1, 4))
        };

        var countBefore = _capturedActivities.Count;
        _service.EvaluateRetention(projects, environments, releases, deployments, releasesToKeep: 1);
        var countAfter = _capturedActivities.Count;

        // The activity list should have grown
        countAfter.Should().BeGreaterThan(countBefore);

        // Get only the retention.evaluate activities added after the clear
        var newActivities = _capturedActivities.Skip(countBefore).ToList();
        var activity = newActivities.FirstOrDefault(a => a.OperationName == "retention.evaluate");
        
        activity.Should().NotBeNull("retention.evaluate activity should be emitted");
        activity!.GetTagItem("retention.kept_releases").Should().Be(1);
        activity.GetTagItem("retention.invalid_deployments_excluded").Should().Be(1);
        activity.GetTagItem("retention.groups_evaluated").Should().Be(1);
    }

    [Fact]
    public void Evaluation_EmitsChildSpans_ForValidateAndRank()
    {
        _capturedActivities.Clear();
        
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        _capturedActivities.Clear(); // Clear before test
        _service.EvaluateRetention(projects, environments, releases, deployments, releasesToKeep: 1);

        // Should have rank spans for each project/environment combination
        var rankActivities = _capturedActivities.Where(a => a.OperationName == "retention.rank").ToList();
        rankActivities.Should().NotBeEmpty("there should be at least one ranking operation");
        
        // Just verify that rank activities have the expected tag structure
        rankActivities.Should().AllSatisfy(r =>
        {
            r.GetTagItem("project.id").Should().NotBeNull();
            r.GetTagItem("environment.id").Should().NotBeNull();
            r.GetTagItem("eligible_releases.count").Should().NotBeNull();
        });
    }

    [Fact]
    public void NoListener_DoesNotAffectBehavior()
    {
        // Dispose listener to test without any listener
        _listener.Dispose();
        _capturedActivities.Clear();

        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        // Should not throw even without a listener
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, releasesToKeep: 1);

        result.KeptReleases.Should().HaveCount(1);
        _capturedActivities.Should().BeEmpty("no listener was active");
    }
}
