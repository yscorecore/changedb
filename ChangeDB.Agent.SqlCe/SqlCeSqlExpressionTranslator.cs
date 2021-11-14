using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeSqlExpressionTranslator : ISqlExpressionTranslator
    {

        public static readonly ISqlExpressionTranslator Default = new SqlCeSqlExpressionTranslator();

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression)
        {
            if (sqlExpression.Function.HasValue)
            {
                return sqlExpression.Function.Value switch
                {
                     Function.Uuid=> "gen_random_uuid()",
                     Function.Now=> "current_timestamp()",
                     _=>string.Empty,
                };
            }
            else
            {
                // TODO handle expression
                return sqlExpression.Expression;            
            }

        }

        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            throw new NotImplementedException();
        }
    }
}
