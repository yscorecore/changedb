using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlExpressionTranslator : ISqlExpressionTranslator
    {
        public static readonly ISqlExpressionTranslator Default = new SqlServerSqlExpressionTranslator();

        private static readonly ConcurrentDictionary<string, object> ValueCache =
            new ConcurrentDictionary<string, object>();
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
            else
            {
                var sql = $"select cast({trimmedExpression} as {context.StoreType})";
                var value = ValueCache.GetOrAdd(sql, (s) => context.AgentInfo.Connection.ExecuteScalar(s));
                return new SqlExpressionDescriptor() { Constant = value };
            }
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

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context)
        {
            if (sqlExpression?.Function != null)
            {
                return sqlExpression.Function.Value switch
                {
                    Function.Now => "getdate()",
                    Function.Uuid => "newid()",
                    _ => throw new NotSupportedException($"not supported function {sqlExpression.Function.Value}")
                };
            }
            if (sqlExpression?.Constant != null)
            {
                return ConstantToSqlExpression(sqlExpression.Constant, context);
            }
            else
            {
                return "null";
            }
        }

        private string ConstantToSqlExpression(object constant, SqlExpressionTranslatorContext context)
        {
            if (constant is string str)
            {
                return Repr(str);
            }
            else if (constant is bool)
            {
                return Convert.ToInt32(constant).ToString();
            }
            else if (constant is double || constant is float || constant is long || constant is int ||
                     constant is short || constant is char || constant is byte || constant is decimal)
            {
                return constant.ToString();
            }
            else if (constant is Guid guid)
            {
                return $"'{guid}'";
            }
            else if (constant is byte[] bytes)
            {
                return $"0x{string.Join("", bytes.Select(p => p.ToString("X2")))}";
            }
            else if (constant is DateTime dateTime)
            {
                return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'"; ;
            }
            else if (constant is DateTimeOffset dateTimeOffset)
            {
                return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss zzz}'";
            }
            else
            {
                return constant.ToString();
            }
        }

        private string Repr(string input)
        {
            if (input is null) return null;
            return $"'{input.Replace("'", "''")}'";
        }

    }
}
