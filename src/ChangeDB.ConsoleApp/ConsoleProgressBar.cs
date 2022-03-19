using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ChangeDB.ConsoleApp
{

    public class ConsoleProgressBarManager
    {
        private record ConsoleProgressBar
        {
            public string Key { get; init; }
            public string Text { get; set; }
            public long Total { get; set; }
            public long Value { get; set; }
            public bool Finished { get; set; }

            private string BuildFirstLine(int totalWidth)
            {
                var text = this.Text ?? string.Empty;
                return text.Length > totalWidth ? text[..totalWidth] : text;
            }

            private string BuildSecondLine(int totalWidth)
            {
                var barWidth = Math.Max(0, totalWidth - 32);
                var percent = (Total == 0 || Finished) ? 1.0 : 1.0 * Value / Total;
                int finished = (int)Math.Round(barWidth * percent);
                string finishedChars = finished > 0 ? new string('=', finished - 1) + '>' : string.Empty;
                var barText = $"[{finishedChars.PadRight(barWidth, ' ')}]";
                return $"{percent,7:p1}{barText}({Value}/{Total})";
            }

            public string[] BuildLines(int totalWidth)
            {
                return new[] { this.BuildFirstLine(totalWidth), this.BuildSecondLine(totalWidth) };
            }


        }

        private readonly List<ConsoleProgressBar> _bars = new List<ConsoleProgressBar>();


        private readonly Timer _timer = new Timer() { Interval = 300, Enabled = false };

        private readonly object _locker = new object();
        public ConsoleProgressBarManager()
        {
            _timer.Elapsed += (s, e) => { this.RenderProgressBars(); };
        }

        public void ReportProgress(string key, string text, long total, long value, bool finished)
        {
            lock (_locker)
            {
                var bar = this._bars.FirstOrDefault(p => p.Key == key);
                if (bar == null)
                {
                    _bars.Add(new ConsoleProgressBar() { Key = key, Text = text, Total = total, Value = value, Finished = finished });
                }
                else
                {
                    bar.Text = text;
                    bar.Total = total;
                    bar.Value = value;
                    bar.Finished = finished;
                }
            }
        }

        public void Start()
        {
            this._timer.Start();
            Console.CursorVisible = false;
        }
        public void End()
        {
            this._timer.Stop();
            RenderProgressBars();
            Console.CursorVisible = true;

        }

        private string[] workingText = new string[0];

        private void RenderProgressBars()
        {
            lock (_locker)
            {
                var consoleTop = Console.CursorTop;
                var goBackLines = CalcGoBackLines();
                CleanPreviousLines(consoleTop, goBackLines);
                Console.SetCursorPosition(0, Math.Max(0, consoleTop - goBackLines));
                // finished bars
                var finished = this._bars.Where(p => p.Finished).ToList();
                this.WriteLines(finished.SelectMany(p => p.BuildLines(Console.WindowWidth)).ToArray());

                finished.ForEach(p => _bars.Remove(p));
                // working bar
                workingText = _bars.SelectMany(p => p.BuildLines(Console.BufferWidth)).ToArray();
                this.WriteLines(workingText);
            }
        }

        private void WriteLines(string[] lines)
        {
            if (lines?.Length > 0)
            {
                Console.WriteLine(string.Join(Environment.NewLine, lines));
            }
        }

        private void CleanPreviousLines(int currentCursorTop, int lineCount)
        {
            if (lineCount > 0)
            {
                Console.SetCursorPosition(0, Math.Max(0, currentCursorTop - lineCount));
                var whiteSpaceLine = new string(' ', Console.WindowWidth);
                var whiteSpaceLines = string.Join(Environment.NewLine,
                    Enumerable.Range(0, lineCount).Select(p => whiteSpaceLine));
                Console.WriteLine(whiteSpaceLines);
            }
        }

        private int CalcGoBackLines()
        {
            if (this.workingText == null) return 0;
            int windowWidth = Console.WindowWidth;
            return this.workingText.Select(p => (int)Math.Ceiling(1.0 * p.Length / windowWidth)).Sum();
        }

    }
}
