namespace ChangeDB
{
    public record SchemaDescriptor : INameObject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }
}
