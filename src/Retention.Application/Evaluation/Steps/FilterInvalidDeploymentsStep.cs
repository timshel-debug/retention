using Retention.Application.Mapping;
using Retention.Application.Models;
using Retention.Application.Specifications;
using Retention.Domain.Entities;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 3: Filters deployments through the validity specification.
/// Produces valid deployments and diagnostic entries for invalid ones.
/// </summary>
public sealed class FilterInvalidDeploymentsStep : IEvaluationStep
{
    private readonly IDeploymentValiditySpecification _spec;
    private readonly IDecisionLogAssembler _assembler;

    public FilterInvalidDeploymentsStep(
        IDeploymentValiditySpecification spec,
        IDecisionLogAssembler assembler)
    {
        _spec = spec;
        _assembler = assembler;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        var index = context.ReferenceIndex!;
        var validDeployments = new List<Deployment>();
        var diagnosticEntries = new List<DecisionLogEntry>();

        foreach (var deployment in context.Deployments)
        {
            var validity = _spec.Evaluate(deployment, index);

            if (validity.IsValid)
            {
                validDeployments.Add(deployment);
            }
            else
            {
                // Determine projectId for diagnostic: use release's ProjectId if resolvable, else "unknown"
                var releaseForDiag = index.ReleasesById.GetValueOrDefault(deployment.ReleaseId);
                var projectId = releaseForDiag?.ProjectId ?? "unknown";

                diagnosticEntries.Add(_assembler.BuildInvalidDeploymentEntry(
                    deployment, projectId, context.ReleasesToKeep, validity.Reasons, context.CorrelationId));
            }
        }

        context.ValidDeployments = validDeployments;
        context.DiagnosticDecisionEntries = diagnosticEntries;
        context.InvalidExcludedCount = diagnosticEntries.Count;
    }
}
