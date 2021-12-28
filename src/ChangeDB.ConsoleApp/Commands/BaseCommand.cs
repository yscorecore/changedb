using System;

namespace ChangeDB.ConsoleApp.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string CommandName { get; }

        public int Run()
        {
            var startTime = DateTime.Now;
            try
            {
                OnRunCommand();
                var totalTime = DateTime.Now - startTime;
                WriteLineWithColor(ConsoleColor.DarkGreen,
                    $"Execute {CommandName} succeeded, total time: {FormatTimeSpan(totalTime)}.");
                return 0;
            }
            catch
            {
                WriteLineWithColor(ConsoleColor.DarkRed, $"Execute {CommandName} failed.");
                throw;
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan) => $"{timeSpan.Hours} hour {timeSpan.Minutes} min {timeSpan.Seconds} sec";

        protected abstract void OnRunCommand();

        private void WriteLineWithColor(ConsoleColor foreColor, string text)
        {
            var backColor = Console.ForegroundColor;
            Console.ForegroundColor = foreColor;
            Console.WriteLine();
            Console.WriteLine(text);
            Console.WriteLine();
            Console.ForegroundColor = backColor;
        }
    }
}
