namespace ChangeDB
{
    public class SequenceDescriptor : INameObject
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string StoreType { get; set; }
        public long? StartValue { get; set; }
        public int? IncrementBy { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }

        public bool? IsCyclic { get; set; }
    }


}
