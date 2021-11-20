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
            throw new NotImplementedException();

        }

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context)
        {
            throw new NotImplementedException();
        }

        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            throw new NotImplementedException();
        }

        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, SqlExpressionTranslatorContext context)
        {
            throw new NotImplementedException();
        }
    }
}
