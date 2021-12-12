namespace ChangeDB.Dump
{
    public record SqlScriptInfo
    {
        public string SqlScriptFile { get; set; }
        public string DatabaseType { get; set; }
    }
}
