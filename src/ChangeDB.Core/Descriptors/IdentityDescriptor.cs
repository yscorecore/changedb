namespace ChangeDB
{
    public record IdentityDescriptor : ExtensionObject
    {
        public virtual long StartValue { get; set; } = 1;
        public virtual int IncrementBy { get; set; } = 1;
        public virtual long? MinValue { get; set; }
        public virtual long? MaxValue { get; set; }
        public virtual bool IsCyclic { get; set; }
        public virtual long? CurrentValue { get; set; }
    }
}
