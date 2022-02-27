using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace ChangeDB
{
    public static class Shell
    {
        public static int Exec(string fileName, string arguments, IDictionary<string, object> envs = null, int maxTimeOutSeconds = 60 * 30)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,

            };
            if (envs != null)
            {
                foreach (var kv in envs)
                {
                    startInfo.Environment.Add(kv.Key, Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            using (var process = Process.Start(startInfo))
            {

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            stringBuilder.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            stringBuilder.AppendLine(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var timeout = maxTimeOutSeconds * 1000;
                    if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))
                    {
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Exec process exit with code: {process.ExitCode}, output: {stringBuilder}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Exec process timeout, total seconds > {maxTimeOutSeconds}s, output: {stringBuilder}");
                    }
                }
                return process.ExitCode;
            }

        }


    }
}
