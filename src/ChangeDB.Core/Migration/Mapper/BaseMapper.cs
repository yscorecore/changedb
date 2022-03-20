namespace ChangeDB.Migration.Mapper
{
    public class BaseMapper<T>
    {
        public T Source { get; set; }
        public T Target { get; set; }
    }
}
