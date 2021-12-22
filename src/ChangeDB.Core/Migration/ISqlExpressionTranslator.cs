using System.Data.Common;
using ChangeDB.Descriptors;

namespace ChangeDB.Migration
{
    public interface ISqlExpressionTranslator
    {
        SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, SqlExpressionTranslatorContext context);

        string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, SqlExpressionTranslatorContext context);
    }
    public record SqlExpressionTranslatorContext
    {
        public SqlExpressionTranslatorContext()
        {

        }
        public string StoreType { get; set; }

        public AgentRunTimeInfo AgentInfo { get; set; }

        public DbConnection Connection { get; set; }

    }
}
