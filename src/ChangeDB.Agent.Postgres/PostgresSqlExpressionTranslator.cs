using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresSqlExpressionTranslator : ISqlExpressionTranslator
    {

        public static readonly ISqlExpressionTranslator Default = new PostgresSqlExpressionTranslator();

        private static readonly ConcurrentDictionary<string, object> ValueCache =
            new ConcurrentDictionary<string, object>();

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context)
        {
            return FromCommonSqlExpressionInternal(sqlExpression, context);
        }
        private string FromCommonSqlExpressionInternal(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context)
        {
            if (sqlExpression?.Function != null)
            {
                return sqlExpression.Function.Value switch
                {
                    Function.Uuid => "gen_random_uuid()",
                    Function.Now => "now()",
                    _ => throw new NotSupportedException($"not supported function {sqlExpression.Function.Value}")
                };
            }
            if ("boolean".Equals(context.StoreType, StringComparison.InvariantCultureIgnoreCase) && sqlExpression?.Constant != null)
            {
                return Convert.ToBoolean(sqlExpression.Constant).ToString().ToLowerInvariant();
            }


            var text = PostgresRepr.ReprConstant(sqlExpression?.Constant);
            if (sqlExpression?.Constant is Guid ||
                sqlExpression?.Constant is DateTime ||
                sqlExpression?.Constant is byte[] ||
                sqlExpression?.Constant is DateTimeOffset)
            {
                return $"{text}::{context.StoreType}";
            }
            return text;


        }


        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, SqlExpressionTranslatorContext context)
        {
            if (string.IsNullOrEmpty(sqlExpression))
            {
                return null;
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
                    _ => new SqlExpressionDescriptor { Constant = sqlExpression }
                };
            }

            var sql = $"select cast({sqlExpression} as {context.StoreType})";
            var val = ValueCache.GetOrAdd(sql, (s) => context.Connection.ExecuteScalar(s));
            return new SqlExpressionDescriptor { Constant = val };
        }

        private string ReplaceTypeConvert(string sqlExpression)
        {
            return Regex.Replace(sqlExpression, @"::(\w+)(\(\d+\)|\(\d+,\s*\d+\))?(\s+(\w+))*", "");
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
