using System;
using System.Collections.Concurrent;
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

            if (sqlExpression?.Constant != null)
            {
                return ConstantToSqlExpression(sqlExpression.Constant, context);
            }

            return "null";


        }
        private string Repr(string input)
        {
            if (input is null) return null;
            return $"'{input.Replace("'", "''")}'";
        }

        private string ConstantToSqlExpression(object constant, SqlExpressionTranslatorContext context)
        {
            if (constant is string str)
            {
                return Repr(str);
            }
            else if (constant is double || constant is float || constant is long || constant is int ||
                     constant is short || constant is char || constant is byte || constant is decimal || constant is bool)
            {
                if ("boolean".Equals(context.StoreType, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Convert.ToBoolean(constant).ToString().ToLowerInvariant();
                }
                else
                {
                    return constant.ToString();
                }


            }
            else if (constant is Guid guid)
            {
                return $"'{guid}'::{context.StoreType}";
            }
            else if (constant is byte[] bytes)
            {
                return $"'\\x{string.Join("", bytes.Select(p => p.ToString("X2")))}'::bytea";
            }
            else if (constant is DateTime dateTime)
            {
                return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'::{context.StoreType}"; ;
            }
            else if (constant is DateTimeOffset dateTimeOffset)
            {
                return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss zzz}'::{context.StoreType}";
            }
            else
            {
                return constant.ToString();
            }
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
            var val = ValueCache.GetOrAdd(sql, (s) => context.AgentInfo.Connection.ExecuteScalar(s));
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
