using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChangeDB
{
    [Collection(nameof(DatabaseEnvironment))]
    public class BaseTest
    {
        static JsonSerializerOptions serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyFields = true,
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = {
               new JsonStringEnumConverter()
            }
        };
        public BaseTest()
        {
            var sc = new ServiceCollection();
            sc.AddChangeDb();
            this.ServiceProvider = sc.BuildServiceProvider();
            this.WriteMode = bool.Parse(Environment.GetEnvironmentVariable("CHANGEDB_WRITE_MODE") ?? "false");
        }
        protected virtual IServiceProvider ServiceProvider { get; }
        protected virtual bool WriteMode { get; }

        private string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, serializeOptions);
        }
        private T Deserialize<T>(string content)
        {
            return JsonSerializer.Deserialize<T>(content, serializeOptions);
        }
        private T DeepClone<T>(T data)
        {
            return Deserialize<T>(Serialize(data));
        }
        protected void WriteToDataFile<T>(T data, string filePath)
        {
            var content = Serialize(data);
            File.WriteAllText(filePath, content);
            Console.WriteLine($"data has beed saved to file {filePath}.");
        }
        protected T ReadFromDataFile<T>(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(content, serializeOptions);
        }
        protected void ShouldBeDataFile<T>(T data, string filePath, Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>> config = null)
        {
            var expectObject = ReadFromDataFile<T>(filePath);
            var formatedData = DeepClone(data);
            Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>> defaultConfig =
                (c) => c.ComparingByMembers<JsonElement>()
                      .ExcludingMissingMembers();
            Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>> allConfig =
                (c) =>
                {
                    return config == null ? defaultConfig(c) : config(defaultConfig(c));
                };
            data.Should().BeEquivalentTo(expectObject, allConfig);

        }

        public static string GetTestCasesFolder()
        {
            return Environment.GetEnvironmentVariable("CHANGEDB_TESTCASES_FOLDER") ?? "testcases";
        }

        public static string GetDatabaseFile(string databaseType, string databaseName)
        {
            return Path.Combine(GetTestCasesFolder(), "databases", databaseType, $"{databaseName}.sql");

        }
    }
}
