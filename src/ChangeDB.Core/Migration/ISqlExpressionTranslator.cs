using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Descriptors;

namespace ChangeDB.Migration
{
    public interface ISqlExpressionTranslator
    {

        SqlExpressionDescriptor ToCommonSqlExpression(string sqlExpression, string storeType, IDbConnection dbConnection);

        string FromCommonSqlExpression(SqlExpressionDescriptor sqlExpression, string storeType);
    }

}
