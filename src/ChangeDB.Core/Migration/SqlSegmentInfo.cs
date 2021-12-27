namespace ChangeDB.Migration
{
    public record SqlSegmentInfo
    {
        public int StartLine { get; set; }
        public int LineCount { get; set; }
        public string Sql { get; set; }
        public int Result { get; set; }
    }
}
