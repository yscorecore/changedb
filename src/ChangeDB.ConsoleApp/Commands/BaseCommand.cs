using System;

namespace ChangeDB.ConsoleApp.Commands
{
    public abstract class BaseCommand : ICommand
    {
        private string FormatTimeSpan(TimeSpan timeSpan) => $"{timeSpan.Hours} hour {timeSpan.Minutes} min {timeSpan.Seconds} sec";
        public abstract string CommandName { get; }

        public int Run()
        {
            var startTime = DateTime.Now;
            this.OnRunCommand();
            var totalTime = DateTime.Now - startTime;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Execute {CommandName} succeeded, total time: {FormatTimeSpan(totalTime)}.");
            return 0;
        }

        protected abstract void OnRunCommand();
    }
}
