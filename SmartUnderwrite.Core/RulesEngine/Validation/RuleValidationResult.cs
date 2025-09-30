namespace SmartUnderwrite.Core.RulesEngine.Validation;

public class RuleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static RuleValidationResult Success()
    {
        return new RuleValidationResult { IsValid = true };
    }

    public static RuleValidationResult Failure(params string[] errors)
    {
        return new RuleValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}