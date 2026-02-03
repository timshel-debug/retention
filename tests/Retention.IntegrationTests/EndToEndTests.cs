using System.Text.Json;
using FluentAssertions;
using Retention.Application;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.IntegrationTests;

/// <summary>
/// End-to-end tests loading sample JSON inputs and asserting expected retention results.
/// These tests simulate the full workflow from JSON parsing to retention evaluation.
/// </summary>
public class EndToEndTests
{
    private readonly EvaluateRetentionService _service = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region JSON DTOs for parsing

    private record ProjectJson(string Id, string Name);
    private record EnvironmentJson(string Id, string Name);
    private record ReleaseJson(string Id, string ProjectId, string? Version, DateTimeOffset Created);
    private record DeploymentJson(string Id, string ReleaseId, string EnvironmentId, DateTimeOffset DeployedAt);

    #endregion

    #region Test Helpers

    private static string GetSampleDataPath(string filename)
    {
        // Navigate from test output to repo root
        var testDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "docs", "retention-original-specification", filename);
    }

    private async Task<(List<Project> Projects, List<Environment> Environments, List<Release> Releases, List<Deployment> Deployments)> 
        LoadSampleDataAsync()
    {
        var projectsPath = GetSampleDataPath("Projects.json");
        var environmentsPath = GetSampleDataPath("Environments.json");
        var releasesPath = GetSampleDataPath("Releases.json");
        var deploymentsPath = GetSampleDataPath("Deployments.json");

        var projectsJson = await File.ReadAllTextAsync(projectsPath);
        var environmentsJson = await File.ReadAllTextAsync(environmentsPath);
        var releasesJson = await File.ReadAllTextAsync(releasesPath);
        var deploymentsJson = await File.ReadAllTextAsync(deploymentsPath);

        var projects = JsonSerializer.Deserialize<List<ProjectJson>>(projectsJson, _jsonOptions)!
            .Select(p => new Project(p.Id, p.Name))
            .ToList();
        
        var environments = JsonSerializer.Deserialize<List<EnvironmentJson>>(environmentsJson, _jsonOptions)!
            .Select(e => new Environment(e.Id, e.Name))
            .ToList();
        
        var releases = JsonSerializer.Deserialize<List<ReleaseJson>>(releasesJson, _jsonOptions)!
            .Select(r => new Release(r.Id, r.ProjectId, r.Version, r.Created))
            .ToList();
        
        var deployments = JsonSerializer.Deserialize<List<DeploymentJson>>(deploymentsJson, _jsonOptions)!
            .Select(d => new Deployment(d.Id, d.ReleaseId, d.EnvironmentId, d.DeployedAt))
            .ToList();

        return (projects, environments, releases, deployments);
    }

    #endregion

    #region Sample data analysis
    
    // Sample data contains:
    // - 2 projects: Project-1 (Random Quotes), Project-2 (Pet Shop)
    // - 2 environments: Environment-1 (Staging), Environment-2 (Production)
    // - 8 releases (Release-8 references non-existent Project-3)
    // - 10 deployments (Deployment-4 references non-existent Environment-3)
    //
    // Valid deployments per (Project, Environment):
    // - (Project-1, Environment-1): Release-1 @ 10:00, Release-2 @ 10:00 (next day)
    // - (Project-1, Environment-2): Release-1 @ 11:00 (day 2)
    // - (Project-2, Environment-1): Release-5 @ 11:00, Release-6 @ 10:00 and 14:00 (day 2), Release-7 @ 13:00 (day 2)
    // - (Project-2, Environment-2): Release-6 @ 11:00 (day 2)
    //
    // Invalid:
    // - Deployment-4: Release-2 to Environment-3 (missing environment)
    // - Deployment-10: Release-8 to Environment-1 (Release-8 belongs to missing Project-3)

    #endregion

    [Fact]
    public async Task SampleData_LoadsCorrectly()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        projects.Should().HaveCount(2);
        environments.Should().HaveCount(2);
        releases.Should().HaveCount(8);
        deployments.Should().HaveCount(10);
    }

    [Fact]
    public async Task SampleData_KeepOne_ReturnsCorrectResults()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // Should have 4 groups: (P1, E1), (P1, E2), (P2, E1), (P2, E2)
        result.Diagnostics.GroupsEvaluated.Should().Be(4);
        result.KeptReleases.Should().HaveCount(4);

        // Project-1, Environment-1: Release-2 (deployed at 2000-01-02T10:00:00)
        result.KeptReleases.Should().Contain(r =>
            r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-2");

        // Project-1, Environment-2: Release-1 (deployed at 2000-01-02T11:00:00)
        result.KeptReleases.Should().Contain(r =>
            r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-2" && r.ReleaseId == "Release-1");

        // Project-2, Environment-1: Release-6 (max deployed at 2000-01-02T14:00:00)
        result.KeptReleases.Should().Contain(r =>
            r.ProjectId == "Project-2" && r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-6");

        // Project-2, Environment-2: Release-6 (deployed at 2000-01-02T11:00:00)
        result.KeptReleases.Should().Contain(r =>
            r.ProjectId == "Project-2" && r.EnvironmentId == "Environment-2" && r.ReleaseId == "Release-6");
    }

    [Fact]
    public async Task SampleData_KeepTwo_ReturnsCorrectResults()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        result.Diagnostics.GroupsEvaluated.Should().Be(4);
        
        // Project-1, Environment-1: Release-2 (rank 1), Release-1 (rank 2)
        var p1e1 = result.KeptReleases.Where(r => r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-1").ToList();
        p1e1.Should().HaveCount(2);
        p1e1.Should().Contain(r => r.ReleaseId == "Release-2" && r.Rank == 1);
        p1e1.Should().Contain(r => r.ReleaseId == "Release-1" && r.Rank == 2);

        // Project-2, Environment-1: Release-6 (rank 1, deployed at 14:00), Release-7 (rank 2, deployed at 13:00)
        var p2e1 = result.KeptReleases.Where(r => r.ProjectId == "Project-2" && r.EnvironmentId == "Environment-1").ToList();
        p2e1.Should().HaveCount(2);
        p2e1.Should().Contain(r => r.ReleaseId == "Release-6" && r.Rank == 1);
        p2e1.Should().Contain(r => r.ReleaseId == "Release-7" && r.Rank == 2);
    }

    [Fact]
    public async Task SampleData_InvalidReferences_ProduceDiagnostics()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // Two invalid deployments:
        // - Deployment-4 references Environment-3 (missing)
        // - Deployment-10 references Release-8 which belongs to Project-3 (missing)
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(2);

        var diagnostics = result.Decisions
            .Where(d => d.ReasonCode == DecisionReasonCodes.InvalidReference)
            .ToList();
        
        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task SampleData_ZeroKeep_ReturnsEmptyButProcessesDiagnostics()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 0);

        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
        
        // Invalid deployments should still be diagnosed
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(2);
    }

    [Fact]
    public async Task SampleData_MultipleRuns_ProduceIdenticalResults()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        var result1 = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        var result2 = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        // Verify determinism
        result1.KeptReleases.Should().BeEquivalentTo(result2.KeptReleases, 
            options => options.WithStrictOrdering());
        result1.Decisions.Should().BeEquivalentTo(result2.Decisions, 
            options => options.WithStrictOrdering());
        result1.Diagnostics.Should().BeEquivalentTo(result2.Diagnostics);
    }

    [Fact]
    public async Task SampleData_ShuffledDeployments_ProduceIdenticalResults()
    {
        var (projects, environments, releases, deployments) = await LoadSampleDataAsync();

        // Shuffle the deployments
        var shuffled = deployments.OrderByDescending(d => d.Id).ToList();

        var result1 = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        var result2 = _service.EvaluateRetention(
            projects, environments, releases, shuffled,
            releasesToKeep: 2);

        result1.KeptReleases.Should().BeEquivalentTo(result2.KeptReleases, 
            options => options.WithStrictOrdering());
    }
}
