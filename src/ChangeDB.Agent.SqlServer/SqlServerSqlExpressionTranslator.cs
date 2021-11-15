using System.Text.RegularExpressions;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using System;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlExpressionTranslator : ISqlExpressionTranslator
    {
        public static ISqlExpressionTranslator Default = new SqlServerSqlExpressionTranslator();
        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            var trimedExpression = TrimBrackets(sqlExpression);
            if (string.IsNullOrEmpty(trimedExpression))
            {
                return null;
            }
            var (matched, function) = IsEmptyArgumentFunction(trimedExpression);
            if (matched)
            {
                return function.ToLower() switch
                {
                    "getdate" => new SqlExpressionDescriptor { Function = Function.Now },
                    "newid" => new SqlExpressionDescriptor { Function = Function.Uuid },
                    _=> throw new NotSupportedException($"not supportd function {function}")
                };
            }
            else
            {
                // TODO handle 
                return new SqlExpressionDescriptor { Expression = trimedExpression };
            }
        }

        private string TrimBrackets(string sqlExpression)
        {
            if (sqlExpression == null) return null;
            var trimedExpression = sqlExpression.Trim();
            if (trimedExpression.StartsWith('(') && trimedExpression.EndsWith(')'))
            {
                return trimedExpression[1..-2];
            }
            return trimedExpression;
        }
        private (bool Success, string Name) IsEmptyArgumentFunction(string expression)
        {
            var match = Regex.Match(expression, @"^(?<func>\w+)\(\s*\)$");
            return (match.Success, match.Groups["func"].Value);
        }

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression)
        {
            throw new System.NotImplementedException();
        }
    }
}
