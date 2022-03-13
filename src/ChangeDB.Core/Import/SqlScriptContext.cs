using System.Data;

namespace ChangeDB.Import
{
    public class SqlScriptContext
    {
        public SqlScriptReader Reader { get; set; }
        public IDbConnection Connection { get; set; }

    }
}
