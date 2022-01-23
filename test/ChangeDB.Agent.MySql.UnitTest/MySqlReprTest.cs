using System;
using System.Collections.Generic;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.MySql.UnitTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class MySqlReprTest
    {
        private readonly DatabaseEnvironment _databaseEnvironment;
        private readonly IRepr _repr = MySqlRepr.Default;
        public MySqlReprTest(DatabaseEnvironment databaseEnvironment)
        {
            _databaseEnvironment = databaseEnvironment;
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("中国")]
        [InlineData("\r\r\n\n\t\b\f'\"\\")]
        [InlineData("____%%%%")]
        [InlineData("Hello\n中国")]

        public void ShouldReprString(string value)
        {
            var reprValue = _repr.ReprValue(value);
            var valueFromDatabase = _databaseEnvironment.DbConnection.ExecuteScalar<string>($"select {reprValue}");
            valueFromDatabase.Should().Be(value);
        }
        //
        // [Theory]
        // [ClassData(typeof(ReprValueTestData))]
        // public void ShouldReprValue(object value)
        // {
        //     var reprValue = _repr.ReprValue(value);
        //     var valueFromDatabase = _databaseEnvironment.DbConnection.ExecuteScalar<object>($"select {reprValue}");
        //     valueFromDatabase.Should().BeEquivalentTo(value);
        // }
        //
        // class ReprValueTestData: List<object[]>
        // {
        //     public ReprValueTestData()
        //     {
        //         Add(new object [] {null});
        //         Add(new object [] {DateTime.Now});
        //         Add(new object [] {12345});
        //         Add(new object [] {new byte[]{1,2,3}});
        //     }
        // }
    }


}