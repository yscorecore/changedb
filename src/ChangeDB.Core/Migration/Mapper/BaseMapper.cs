namespace ChangeDB.Migration.Mapper
{
    public record BaseMapper<T>
    {
        public T Source { get; set; }
        public T Target { get; set; }
    }
}
