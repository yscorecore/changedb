using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    internal class SqlServerSqlExpressionTranslator
    {
        public static readonly SqlServerSqlExpressionTranslator Default = new SqlServerSqlExpressionTranslator();

        private static readonly ConcurrentDictionary<string, object> ValueCache =
            new ConcurrentDictionary<string, object>();


        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, string storeType, IDbConnection dbConnection)
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
                var castType = NormalCastType(storeType);
                var sql = $"select cast({trimmedExpression} as {castType})";
                var value = ValueCache.GetOrAdd(sql, (s) => dbConnection.ExecuteScalar(s));
                return new SqlExpressionDescriptor() { Constant = value };
            }
        }
        private string NormalCastType(string storeType)
        {
            if (storeType.StartsWith("binary", StringComparison.InvariantCultureIgnoreCase))
            {
                return "var" + storeType;
            }
            else if (storeType.StartsWith("nchar", StringComparison.InvariantCultureIgnoreCase))
            {
                return "nvarchar" + storeType.Substring(5);
            }
            else if (storeType.StartsWith("char", StringComparison.InvariantCultureIgnoreCase))
            {
                return "var" + storeType;
            }
            else
            {
                return storeType;
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


        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, string storeType)
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
            return SqlServerRepr.ReprConstant(sqlExpression?.Constant);
        }
    }
}
