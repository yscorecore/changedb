using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Descriptors;

namespace ChangeDB.Migration
{
    public interface ISqlExpressionTranslator
    {
        SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression);

        string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression);
    }
}
