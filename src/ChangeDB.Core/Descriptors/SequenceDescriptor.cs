namespace ChangeDB
{
    public class SequenceDescriptor
    {
        public virtual string Name { get; set; }
        public virtual string Schema { get; set; }
        public virtual string StoreType { get; set; }
        public virtual long? StartValue { get; set; }
        public virtual int? IncrementBy { get; set; }
        public virtual long? MinValue { get; set; }
        public virtual long? MaxValue { get; set; }

        public virtual bool? IsCyclic { get; set; }
    }
}
