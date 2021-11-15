using ChangeDB.Descriptors;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlExpressionTranslator:ISqlExpressionTranslator
    {
        public static ISqlExpressionTranslator Default = new SqlServerSqlExpressionTranslator();
        public SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression)
        {
            throw new System.NotImplementedException();
        }
        

        public string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression)
        {
            throw new System.NotImplementedException();
        }
    }
}
