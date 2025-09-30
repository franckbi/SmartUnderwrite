using System.Linq.Expressions;
using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IExpressionCompiler
{
    /// <summary>
    /// Compiles a condition string into an executable expression
    /// </summary>
    /// <param name="condition">The condition string to compile</param>
    /// <returns>Compiled expression that can be executed against an EvaluationContext</returns>
    /// <exception cref="ArgumentException">Thrown when condition syntax is invalid</exception>
    Expression<Func<EvaluationContext, bool>> CompileCondition(string condition);

    /// <summary>
    /// Validates that a condition string can be compiled successfully
    /// </summary>
    /// <param name="condition">The condition string to validate</param>
    /// <returns>True if the condition can be compiled, false otherwise</returns>
    bool ValidateCondition(string condition);

    /// <summary>
    /// Gets a list of available properties that can be used in conditions
    /// </summary>
    /// <returns>List of property names available for use in expressions</returns>
    IEnumerable<string> GetAvailableProperties();
}