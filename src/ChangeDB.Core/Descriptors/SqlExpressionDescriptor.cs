namespace ChangeDB.Descriptors
{
    public record SqlExpressionDescriptor : ExtensionObject
    {
        public Function? Function { get; set; }

        public object Constant { get; set; }
    }
    public enum Function
    {
        Now,
        Uuid,
    }
}
