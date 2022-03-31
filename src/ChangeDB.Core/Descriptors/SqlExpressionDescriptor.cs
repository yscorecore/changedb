namespace ChangeDB.Descriptors
{
    public class SqlExpressionDescriptor : ExtensionObject
    {
        public Function? Function { get; set; }

        public object Constant { get; set; }

        public static SqlExpressionDescriptor FromConstant(object constant)
        {
            return new SqlExpressionDescriptor { Constant = constant };
        }

        public static SqlExpressionDescriptor FromFunction(Function function)
        {
            return new SqlExpressionDescriptor() { Function = function };
        }
    }
    public enum Function
    {
        Now,
        Uuid,
    }
}
