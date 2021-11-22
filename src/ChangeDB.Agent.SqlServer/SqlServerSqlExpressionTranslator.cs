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
        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, SqlExpressionTranslatorContext context)
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
                    _ => new SqlExpressionDescriptor { Constant = trimmedExpression }
                };
            }
            if (context.StoreType?.ToLower() == "bit" && Regex.IsMatch(trimmedExpression, @"^\d+$"))
            {
                var val = Convert.ToBoolean(int.Parse(trimmedExpression));
                return new SqlExpressionDescriptor { Constant = val.ToString().ToLowerInvariant() };
            }
            return new SqlExpressionDescriptor { Constant = trimmedExpression };
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
        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context)
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
                if (KeywordMapper.TryGetValue(sqlExpression.Constant ?? string.Empty, out var mappedExpression))
                {
                    return mappedExpression;
                }
                // TODO Need to handle complex expression
                return sqlExpression.Constant;
            }
        }
    }
}
