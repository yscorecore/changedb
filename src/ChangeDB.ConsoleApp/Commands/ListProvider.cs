using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.ConsoleApp.Commands
{
    [Verb(Command, HelpText = "list all supported databases, and show connection string template")]
    public class ListProvider: BaseCommand
    {
        
        private const string Command = "list-provider";
        public override string CommandName => Command;
        protected override void OnRunCommand()
        {
            var title = new[] {"Database", "Support Platform", "Connection String Template"};
            var agentFactory = ServiceHost.Default.GetRequiredService<IAgentFactory>();
            var rows = agentFactory.ListAll().Select(p=>p.AgentSetting)
                .Select(p=>new []{p.DatabaseType,p.SupportOs.ToString(),p.ConnectionTemplate}).ToList();
            var consoleTable = new ConsoleTable(title);
           rows.ForEach(row=>consoleTable.AddRow(row));
           consoleTable.Write();
            //new ConsoleTableWriter(Console.WindowWidth).WriteData(title,rows);
        }
        
    }

    public class ConsoleTableWriter
    {
        private readonly int _width;

        public ConsoleTableWriter(int width)
        {
            _width = width;
        }

        public void WriteData(string[] title, IList<string[]> lines)
        {
           var widths = CalcColumnWidths(title, lines);
           
        }

        private static int[] CalcColumnWidths(string[] title, IList<string[]> lines)
        {
            int[] widths = title.Select(p => p?.Length ?? 0).ToArray();
            foreach (var line in lines)
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    if (line[i]?.Length > widths[i])
                    {
                        widths[i] = line[i].Length;
                    }
                }
            }
            return widths;
        }
    }
}
