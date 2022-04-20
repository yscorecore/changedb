using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Postgres
{
    internal class PostgresSqlExpressionTranslator
    {

        public static readonly PostgresSqlExpressionTranslator Default = new PostgresSqlExpressionTranslator();

        private static readonly ConcurrentDictionary<string, object> ValueCache =
            new ConcurrentDictionary<string, object>();



        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, string storeType)
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
            if ("boolean".Equals(storeType, StringComparison.InvariantCultureIgnoreCase) && sqlExpression?.Constant != null)
            {
                return Convert.ToBoolean(sqlExpression.Constant).ToString().ToLowerInvariant();
            }


            var text = PostgresRepr.ReprConstant(sqlExpression?.Constant);
            if (sqlExpression?.Constant is Guid or DateTime or byte[] or DateTimeOffset)
            {
                return $"{text}::{storeType}";
            }
            return text;


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

        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, string storeType, IDbConnection dbConnection)
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

            var sql = $"select cast({sqlExpression} as {storeType})";
            var val = ValueCache.GetOrAdd(sql, (s) => dbConnection.ExecuteScalar(s));
            return new SqlExpressionDescriptor { Constant = val };
        }
    }
}
