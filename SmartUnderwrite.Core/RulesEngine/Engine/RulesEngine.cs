using Microsoft.Extensions.Logging;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Core.RulesEngine.Engine;

public class RulesEngine : IRulesEngine
{
    private readonly IRuleParser _ruleParser;
    private readonly IExpressionCompiler _expressionCompiler;
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RulesEngine> _logger;

    public RulesEngine(
        IRuleParser ruleParser,
        IExpressionCompiler expressionCompiler,
        IRuleRepository ruleRepository,
        ILogger<RulesEngine> logger)
    {
        _ruleParser = ruleParser ?? throw new ArgumentNullException(nameof(ruleParser));
        _expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EvaluationResult> EvaluateAsync(LoanApplication application, Applicant applicant)
    {
        var activeRules = await GetActiveRulesAsync();
        return await EvaluateAsync(application, applicant, activeRules);
    }

    public async Task<EvaluationResult> EvaluateAsync(LoanApplication application, Applicant applicant, IEnumerable<Rule> rules)
    {
        _logger.LogInformation("Starting evaluation for application {ApplicationId} with {RuleCount} rules", 
            application.Id, rules.Count());

        var context = CreateEvaluationContext(application, applicant);
        var result = new EvaluationResult
        {
            Outcome = DecisionOutcome.Approve, // Default to approve unless rules say otherwise
            Score = 0,
            Reasons = new List<string>(),
            RuleResults = new List<RuleExecutionResult>()
        };

        var orderedRules = rules.OrderBy(r => r.Priority).ToList();
        
        foreach (var rule in orderedRules)
        {
            var ruleResult = await EvaluateRuleAsync(rule, context);
            result.RuleResults.Add(ruleResult);

            if (ruleResult.Executed)
            {
                // Apply rule outcome with priority-based conflict resolution
                if (ruleResult.Outcome.HasValue)
                {
                    result.Outcome = ResolveConflict(result.Outcome, ruleResult.Outcome.Value);
                }

                // Add reason if provided
                if (!string.IsNullOrEmpty(ruleResult.Reason))
                {
                    result.Reasons.Add(ruleResult.Reason);
                }

                // Apply score impact
                result.Score += ruleResult.ScoreImpact;
            }

            // Log any errors
            if (ruleResult.Errors.Any())
            {
                _logger.LogWarning("Rule {RuleName} had errors: {Errors}", 
                    rule.Name, string.Join(", ", ruleResult.Errors));
            }
        }

        // Ensure score is non-negative
        result.Score = Math.Max(0, result.Score);

        _logger.LogInformation("Evaluation completed for application {ApplicationId}. Outcome: {Outcome}, Score: {Score}", 
            application.Id, result.Outcome, result.Score);

        return result;
    }

    public async Task<bool> ValidateRuleDefinitionAsync(string ruleJson)
    {
        try
        {
            var validationResult = _ruleParser.ValidateRuleJson(ruleJson);
            return validationResult.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rule definition");
            return false;
        }
    }

    public async Task<IEnumerable<Rule>> GetActiveRulesAsync()
    {
        return await _ruleRepository.GetActiveRulesAsync();
    }

    private EvaluationContext CreateEvaluationContext(LoanApplication application, Applicant applicant)
    {
        return new EvaluationContext
        {
            Amount = application.Amount,
            IncomeMonthly = application.IncomeMonthly,
            CreditScore = application.CreditScore,
            EmploymentType = application.EmploymentType,
            ProductType = application.ProductType,
            ApplicationDate = application.CreatedAt
        };
    }

    private async Task<RuleExecutionResult> EvaluateRuleAsync(Rule rule, EvaluationContext context)
    {
        var result = new RuleExecutionResult
        {
            RuleName = rule.Name,
            Executed = false,
            Errors = new List<string>()
        };

        try
        {
            var ruleDefinition = _ruleParser.ParseRuleDefinition(rule.RuleDefinition);
            
            // Evaluate clauses
            foreach (var clause in ruleDefinition.Clauses)
            {
                if (EvaluateClause(clause, context))
                {
                    result.Executed = true;
                    result.Outcome = ParseAction(clause.Action);
                    result.Reason = clause.Reason;
                    break; // First matching clause wins
                }
            }

            // Calculate score impact
            if (ruleDefinition.Score != null)
            {
                result.ScoreImpact = CalculateScoreImpact(ruleDefinition.Score, context);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Rule execution error: {ex.Message}");
            _logger.LogError(ex, "Error executing rule {RuleName}", rule.Name);
        }

        return result;
    }

    private bool EvaluateClause(RuleClause clause, EvaluationContext context)
    {
        try
        {
            var expression = _expressionCompiler.CompileCondition(clause.Condition);
            var compiledExpression = expression.Compile();
            return compiledExpression(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating clause condition: {Condition}", clause.Condition);
            return false;
        }
    }

    private int CalculateScoreImpact(ScoreDefinition scoreDefinition, EvaluationContext context)
    {
        int impact = scoreDefinition.Base;

        // Apply additions
        foreach (var modifier in scoreDefinition.Add)
        {
            if (EvaluateScoreModifier(modifier, context))
            {
                impact += modifier.Points;
            }
        }

        // Apply subtractions
        foreach (var modifier in scoreDefinition.Subtract)
        {
            if (EvaluateScoreModifier(modifier, context))
            {
                impact -= modifier.Points;
            }
        }

        return impact;
    }

    private bool EvaluateScoreModifier(ScoreModifier modifier, EvaluationContext context)
    {
        try
        {
            var expression = _expressionCompiler.CompileCondition(modifier.Condition);
            var compiledExpression = expression.Compile();
            return compiledExpression(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating score modifier condition: {Condition}", modifier.Condition);
            return false;
        }
    }

    private DecisionOutcome ParseAction(string action)
    {
        return action.ToUpperInvariant() switch
        {
            "APPROVE" => DecisionOutcome.Approve,
            "REJECT" => DecisionOutcome.Reject,
            "MANUAL" => DecisionOutcome.ManualReview,
            _ => DecisionOutcome.ManualReview // Default to manual review for unknown actions
        };
    }

    private DecisionOutcome ResolveConflict(DecisionOutcome current, DecisionOutcome newOutcome)
    {
        // Priority order: Reject > ManualReview > Approve
        // Once rejected or flagged for manual review, it cannot be overridden to approve
        return (current, newOutcome) switch
        {
            (DecisionOutcome.Reject, _) => DecisionOutcome.Reject,
            (_, DecisionOutcome.Reject) => DecisionOutcome.Reject,
            (DecisionOutcome.ManualReview, _) => DecisionOutcome.ManualReview,
            (_, DecisionOutcome.ManualReview) => DecisionOutcome.ManualReview,
            _ => newOutcome
        };
    }
}