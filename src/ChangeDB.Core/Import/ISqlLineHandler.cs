namespace ChangeDB.Import
{
    public interface ISqlLineHandler
    {
        void Handle(SqlScriptContext context);
    }
}
