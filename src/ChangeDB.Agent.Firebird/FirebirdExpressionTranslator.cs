using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.Firebird
{
    public class FirebirdExpressionTranslator : ISqlExpressionTranslator
    {

        public static readonly ISqlExpressionTranslator Default = new FirebirdExpressionTranslator();

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
                    Function.Uuid => "(UUID_TO_BIN(UUID()))",
                    Function.Now => "(NOW(6))",
                    _ => throw new NotSupportedException($"not supported function {sqlExpression.Function.Value}")
                };
            }
            var text = FirebirdRepr.ReprConstant(sqlExpression?.Constant);
            if (sqlExpression?.Constant is byte[] || sqlExpression?.Constant is Guid)
            {
                return $"({text})";
            }
            return text;
        }


        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, SqlExpressionTranslatorContext context)
        {
            if (string.IsNullOrWhiteSpace(sqlExpression))
            {
                return null;
            }

            sqlExpression = sqlExpression.Trim();
            if (IsNumber(sqlExpression) || IsString(sqlExpression))
            {
                return ConstantValue(sqlExpression);
            }
            else if (IsKeyword(sqlExpression))
            {
                if ("CURRENT_TIMESTAMP".Equals(sqlExpression, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new SqlExpressionDescriptor { Function = Function.Now };
                }
                else
                {
                    return ConstantValue(sqlExpression);
                }
            }
            else if (IsFunction(sqlExpression))
            {
                var funcExpression = sqlExpression.Substring(1, sqlExpression.Length - 2);
                if (IsFunctionNow(funcExpression))
                {
                    return new SqlExpressionDescriptor { Function = Function.Now };
                }
                else if (IsFunctionUuid(funcExpression))
                {
                    return new SqlExpressionDescriptor { Function = Function.Uuid };
                }
                else
                {
                    throw new NotSupportedException($"not support function expression '{funcExpression}'.");
                }
            }
            else
            {
                // bit type b'01010101'
                return ConstantValue(sqlExpression, context.StoreType);
            }
            bool IsString(string expression)
            {
                return expression.StartsWith('\'') && expression.EndsWith('\'');
            }

            bool IsNumber(string expression)
            {
                return Regex.IsMatch(expression, @"^\d+(\.\d+)?$");
            }

            bool IsKeyword(string expression)
            {
                return Regex.IsMatch(expression, @"^\w+$");
            }

            bool IsFunctionNow(string expression)
            {
                var match = Regex.Match(expression, @"^(?<func>\w+)\(\d?\)$");
                if (match.Success)
                {
                    var funcName = match.Groups["func"].Value;
                    return funcName.Equals("now", StringComparison.InvariantCultureIgnoreCase) ||
                           funcName.Equals("CURRENT_TIMESTAMP", StringComparison.InvariantCultureIgnoreCase);
                }
                return false;
            }

            bool IsFunctionUuid(string expression)
            {
                return "UUID_TO_BIN(UUID())".Equals(expression, StringComparison.InvariantCultureIgnoreCase);
            }

            SqlExpressionDescriptor ConstantValue(string expression, string storeType = null)
            {
                var sql = string.IsNullOrEmpty(storeType) ? $"select {expression}" : $"select cast({expression} as {storeType})";
                var val = ValueCache.GetOrAdd(sql, (s) => context.Connection.ExecuteScalar(s));
                return new SqlExpressionDescriptor { Constant = val };
            }

            bool IsFunction(string expression)
            {
                return expression.StartsWith('(') && expression.EndsWith(')');
            }
        }
    }
}