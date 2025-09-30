using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Core.RulesEngine.Compilation;

public class ExpressionCompiler : IExpressionCompiler
{
    private static readonly Dictionary<string, PropertyInfo> AvailableProperties;
    private static readonly Dictionary<string, Func<Expression, Expression, BinaryExpression>> BinaryOperators;
    private static readonly Regex TokenRegex = new(@"(\w+|\d+\.?\d*|[<>=!&|()]+|""[^""]*"")", RegexOptions.Compiled);

    static ExpressionCompiler()
    {
        AvailableProperties = typeof(EvaluationContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        BinaryOperators = new Dictionary<string, Func<Expression, Expression, BinaryExpression>>
        {
            { "==", Expression.Equal },
            { "!=", Expression.NotEqual },
            { "<", Expression.LessThan },
            { "<=", Expression.LessThanOrEqual },
            { ">", Expression.GreaterThan },
            { ">=", Expression.GreaterThanOrEqual },
            { "&&", Expression.AndAlso },
            { "||", Expression.OrElse }
        };
    }

    public Expression<Func<EvaluationContext, bool>> CompileCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("Condition cannot be null or empty", nameof(condition));

        try
        {
            var parameter = Expression.Parameter(typeof(EvaluationContext), "context");
            var body = ParseExpression(condition, parameter);
            return Expression.Lambda<Func<EvaluationContext, bool>>(body, parameter);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to compile condition '{condition}': {ex.Message}", nameof(condition), ex);
        }
    }

    public bool ValidateCondition(string condition)
    {
        try
        {
            CompileCondition(condition);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> GetAvailableProperties()
    {
        return AvailableProperties.Keys;
    }

    private Expression ParseExpression(string condition, ParameterExpression parameter)
    {
        var tokens = TokenizeCondition(condition);
        return ParseOrExpression(tokens, parameter);
    }

    private List<string> TokenizeCondition(string condition)
    {
        var matches = TokenRegex.Matches(condition);
        return matches.Cast<Match>().Select(m => m.Value.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    private Expression ParseOrExpression(List<string> tokens, ParameterExpression parameter)
    {
        var left = ParseAndExpression(tokens, parameter);

        while (tokens.Count > 0 && tokens[0] == "||")
        {
            tokens.RemoveAt(0); // Remove ||
            var right = ParseAndExpression(tokens, parameter);
            left = Expression.OrElse(left, right);
        }

        return left;
    }

    private Expression ParseAndExpression(List<string> tokens, ParameterExpression parameter)
    {
        var left = ParseComparisonExpression(tokens, parameter);

        while (tokens.Count > 0 && tokens[0] == "&&")
        {
            tokens.RemoveAt(0); // Remove &&
            var right = ParseComparisonExpression(tokens, parameter);
            left = Expression.AndAlso(left, right);
        }

        return left;
    }

    private Expression ParseComparisonExpression(List<string> tokens, ParameterExpression parameter)
    {
        var left = ParsePrimaryExpression(tokens, parameter);

        if (tokens.Count > 0 && BinaryOperators.ContainsKey(tokens[0]) && tokens[0] != "&&" && tokens[0] != "||")
        {
            var operatorToken = tokens[0];
            tokens.RemoveAt(0);
            var right = ParsePrimaryExpression(tokens, parameter);

            // Handle nullable comparisons
            if (IsNullableComparison(left, right))
            {
                return CreateNullableComparison(left, right, operatorToken);
            }

            return BinaryOperators[operatorToken](left, right);
        }

        return left;
    }

    private Expression ParsePrimaryExpression(List<string> tokens, ParameterExpression parameter)
    {
        if (tokens.Count == 0)
            throw new ArgumentException("Unexpected end of expression");

        var token = tokens[0];
        tokens.RemoveAt(0);

        // Handle parentheses
        if (token == "(")
        {
            var expr = ParseOrExpression(tokens, parameter);
            if (tokens.Count == 0 || tokens[0] != ")")
                throw new ArgumentException("Missing closing parenthesis");
            tokens.RemoveAt(0); // Remove )
            return expr;
        }

        // Handle property access
        if (AvailableProperties.ContainsKey(token))
        {
            var property = AvailableProperties[token];
            return Expression.Property(parameter, property);
        }

        // Handle numeric literals
        if (decimal.TryParse(token, out var decimalValue))
        {
            return Expression.Constant(decimalValue);
        }

        if (int.TryParse(token, out var intValue))
        {
            return Expression.Constant(intValue);
        }

        // Handle string literals
        if (token.StartsWith("\"") && token.EndsWith("\""))
        {
            var stringValue = token.Substring(1, token.Length - 2);
            return Expression.Constant(stringValue);
        }

        // Handle boolean literals
        if (bool.TryParse(token, out var boolValue))
        {
            return Expression.Constant(boolValue);
        }

        throw new ArgumentException($"Unknown token: {token}");
    }

    private bool IsNullableComparison(Expression left, Expression right)
    {
        return IsNullableType(left.Type) || IsNullableType(right.Type);
    }

    private bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private Expression CreateNullableComparison(Expression left, Expression right, string operatorToken)
    {
        // Convert both sides to nullable if needed
        if (!IsNullableType(left.Type) && IsNullableType(right.Type))
        {
            left = Expression.Convert(left, right.Type);
        }
        else if (IsNullableType(left.Type) && !IsNullableType(right.Type))
        {
            right = Expression.Convert(right, left.Type);
        }

        return BinaryOperators[operatorToken](left, right);
    }
}