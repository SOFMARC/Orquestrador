namespace AnalyzerOrchestrator.Application.Workflow;

public class WorkflowStepDefinition
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsOptional { get; set; }

    public WorkflowStepDefinition(int stepNumber, string name, string? description = null, bool isOptional = false)
    {
        StepNumber = stepNumber;
        Name = name;
        Description = description;
        IsOptional = isOptional;
    }
}
