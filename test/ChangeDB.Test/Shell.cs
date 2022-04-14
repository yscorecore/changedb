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
        public static (int ExitCode, string OutPut, string Error) Exec(string fileName, string arguments, IDictionary<string, object> envs = null, int maxTimeOutSeconds = 60 * 10)
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
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            using var process = Process.Start(startInfo);
            using AutoResetEvent outputWaitHandle = new AutoResetEvent(false);
            using AutoResetEvent errorWaitHandle = new AutoResetEvent(false);
            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    outputBuilder.AppendLine(e.Data);
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
                    errorBuilder.AppendLine(e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var timeout = maxTimeOutSeconds * 1000;
            if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))
            {
                return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
            else
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
                throw new Exception($"Exec process timeout, total seconds > {maxTimeOutSeconds}s, output: {outputBuilder}");
            }

        }

        public static (int ExitCode, string OutPut, string Error) ExecOrDebug(string fileName, string arguments, IDictionary<string, object> envs = null, int maxTimeOutSeconds = 60 * 30)
        {
            if (Debugger.IsAttached)
            {
                var newDic = envs == null ? new Dictionary<string, object>() : new Dictionary<string, object>(envs);
                newDic["DEBUGING"] = true;
                return Exec(fileName, arguments, newDic, maxTimeOutSeconds);
            }
            else
            {
                return Exec(fileName, arguments, envs, maxTimeOutSeconds);
            }
        }
    }
}
