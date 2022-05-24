using System;

namespace ChangeDB
{
    public record AgentSetting
    {
        public string DatabaseType { get; init; }
        public bool SupportSchema => !string.IsNullOrEmpty(DefaultSchema);
        public string DefaultSchema { get; init; }
        public int ObjectNameMaxLength { get; init; }
        public Func<string, string, string> IdentityName { get; init; }
        
        public string ConnectionTemplate { get; init; }
        
        public Os SupportOs { get;  init; }=Os.All;
    }

    [Flags]
    public enum Os
    {
        Windows=1,
        Linux=2,
        Mac=4,
        All=Windows^Linux^Mac,
    }
}
