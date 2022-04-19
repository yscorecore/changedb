using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Core.Descriptors
{
    public class DataTypeDescriptorTest
    {
        [Theory]
        [InlineData("int", CommonDataType.Int, null, null)]
        [InlineData("varchar(10)", CommonDataType.Varchar, 10, null)]
        [InlineData("decimal(10,3)", CommonDataType.Decimal, 10, 3)]
        [InlineData(" int ", CommonDataType.Int, null, null)]
        [InlineData(" varchar ( 10 ) ", CommonDataType.Varchar, 10, null)]
        [InlineData(" decimal ( 10 ,3 ) ", CommonDataType.Decimal, 10, 3)]
        public void ShouldParseSuccess(string text, CommonDataType dataType, int? arg1, int? arg2)
        {
            var typeDescriptor = DataTypeDescriptor.Parse(text);
            typeDescriptor.Should().NotBeNull();
            typeDescriptor.DbType.Should().Be(dataType);
            typeDescriptor.Arg1.Should().Be(arg1);
            typeDescriptor.Arg2.Should().Be(arg2);
        }
        [Theory]
        [InlineData("int 2", "invalid datatype text")]
        [InlineData("abc(10)", "unknow datatype *")]
        [InlineData("decimal(10)", "arguments not match")]
        public void ShouldParseFail(string text, string error)
        {
            Action action = () => DataTypeDescriptor.Parse(text);
            action.Should().Throw<ArgumentException>()
                .WithMessage(error);

        }
    }
}
