using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresSqlExpressionTranslator : ISqlExpressionTranslator
    {

        public static readonly ISqlExpressionTranslator Default = new PostgresSqlExpressionTranslator();

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression)
        {
            if (sqlExpression.Function.HasValue)
            {
                return sqlExpression.Function.Value switch
                {
                    Function.Uuid => "gen_random_uuid()",
                    Function.Now => "now()",
                };
            }
            else
            {

                // TODO handle expression
                return sqlExpression.Expression;
            }

        }

        private static readonly Dictionary<string, SqlExpressionDescriptor> KeyWordsMap =
            new Dictionary<string, SqlExpressionDescriptor>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["true"] = new SqlExpressionDescriptor { Expression = "1" },
                ["false"] = new SqlExpressionDescriptor { Expression = "0" },
            };
        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            if (string.IsNullOrEmpty(sqlExpression))
            {
                return new SqlExpressionDescriptor { Expression = sqlExpression };
            }

            if (KeyWordsMap.TryGetValue(sqlExpression, out var mappedDesc))
            {
                return mappedDesc;
            }

            sqlExpression = ReplaceTypeConvert(sqlExpression.Trim());
            if (Regex.IsMatch(sqlExpression, @"CURRENT_TIMESTAMP(\(\d\))?", RegexOptions.IgnoreCase))
            {
                return new SqlExpressionDescriptor { Function = Function.Now };
            }

            if (IsEmptyArgumentFunction(sqlExpression, out var function))
            {
                return function.ToLowerInvariant() switch
                {
                    "now" => new SqlExpressionDescriptor { Function = Function.Now },
                    "gen_random_uuid" => new SqlExpressionDescriptor { Function = Function.Uuid },
                    _ => new SqlExpressionDescriptor { Expression = sqlExpression }
                };
            }

            return new SqlExpressionDescriptor { Expression = sqlExpression };
        }

        private string ReplaceTypeConvert(string sqlExpression)
        {
            return Regex.Replace(sqlExpression, @"::(\w+)(\s+(\w+))*", "");
        }

        private bool IsEmptyArgumentFunction(string expression, out string functionName)
        {
            if (string.IsNullOrEmpty(expression))
            {
                functionName = string.Empty;
                return false;
            }

            var match = Regex.Match(expression, @"^(?<func>\w+)\(\s*\)$");
            functionName = match.Groups["func"].Value;
            return match.Success;
        }
    }
}
