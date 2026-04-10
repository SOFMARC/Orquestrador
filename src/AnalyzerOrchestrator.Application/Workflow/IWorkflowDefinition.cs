namespace AnalyzerOrchestrator.Application.Workflow;

public interface IWorkflowDefinition
{
    IReadOnlyList<WorkflowStepDefinition> GetSteps();
}
