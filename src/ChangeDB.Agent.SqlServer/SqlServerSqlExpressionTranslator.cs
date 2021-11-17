using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlExpressionTranslator : ISqlExpressionTranslator
    {
        public static readonly ISqlExpressionTranslator Default = new SqlServerSqlExpressionTranslator();
        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            var trimmedExpression = TrimBrackets(sqlExpression);
            if (string.IsNullOrEmpty(trimmedExpression))
            {
                return null;
            }
            if (IsEmptyArgumentFunction(trimmedExpression, out var function))
            {
                return function.ToLower() switch
                {
                    "getdate" => new SqlExpressionDescriptor { Function = Function.Now },
                    "newid" => new SqlExpressionDescriptor { Function = Function.Uuid },
                    _ => new SqlExpressionDescriptor { Expression = trimmedExpression }
                };
            }
            return new SqlExpressionDescriptor { Expression = trimmedExpression };
        }

        private string TrimBrackets(string sqlExpression)
        {
            if (sqlExpression == null) return null;
            var trimmedExpression = sqlExpression;
            do
            {
                trimmedExpression = trimmedExpression.Trim();
                if (trimmedExpression.StartsWith('(') && trimmedExpression.EndsWith(')'))
                {
                    trimmedExpression = trimmedExpression[1..^1];
                }
                else
                {
                    break;
                }
            } while (true);

            return trimmedExpression;
        }
        private bool IsEmptyArgumentFunction(string expression, out string functionName)
        {
            var match = Regex.Match(expression, @"^(?<func>\w+)\(\s*\)$");
            functionName = match.Groups["func"].Value;
            return match.Success;
        }

        private static readonly Dictionary<string, string> KeywordMapper = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["true"] = "1",
            ["false"] = "0"
        };
        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression)
        {
            if (sqlExpression.Function != null)
            {
                return sqlExpression.Function.Value switch
                {
                    Function.Now => "getdate()",
                    Function.Uuid => "newid()",
                    _ => throw new NotSupportedException($"not supported function {sqlExpression.Function.Value}")
                };
            }
            else
            {
                if (KeywordMapper.TryGetValue(sqlExpression.Expression ?? string.Empty, out var mappedExpression))
                {
                    return mappedExpression;
                }
                // TODO Need to handle complex expression
                return sqlExpression.Expression;
            }
        }
    }
}
