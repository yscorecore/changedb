using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChangeDB
{
    public static class DockerCompose
    {
        public static int MaxTimeOutSeconds { get; set; } = 60 * 30;

        public static IDisposable Up(IDictionary<string, object> envs = null)
        {
            envs ??= new Dictionary<string, object>();
            Exec("docker-compose", "up --build -d", envs);
            return new DockerComposeGroup(null);
        }

        public static IDisposable Up(string dockerComposeFile, IDictionary<string, object> envs, string waitingContainerPorts, int maxTimeOutSeconds = 120)
        {
            var actualDockerComposeFile = FindDockerComposeFile();
            if (actualDockerComposeFile == null)
            {
                throw new FileNotFoundException($"can not find the docker compose file '{dockerComposeFile}'.", dockerComposeFile);
            }
            envs ??= new Dictionary<string, object>();
            var workFolder = Environment.CurrentDirectory;
            var tempFolder = Path.Combine(workFolder, "tmp", DateTime.Now.ToString("yyyyMMdd_HHmmss.fff"));
            Directory.CreateDirectory(tempFolder);
            var statusFile = Path.Combine(tempFolder, "status.txt");
            var waitComposeFile = Path.Combine(tempFolder, "docker-compose.wait.yml");
            // create empty status file
            File.WriteAllText(statusFile, string.Empty);
            // create docker compose file
            GeneratorWaitComposeFile();

            string dockerComposeFileArgument = $"-f \"{actualDockerComposeFile}\" -f \"{waitComposeFile}\"";

            using (AutoResetEvent waitReport = new AutoResetEvent(false))
            {
                using (var fileWatcher = new FileSystemWatcher(tempFolder))
                {
                    fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess;
                    fileWatcher.Changed += (sender, e) =>
                    {
                        if (e.ChangeType == WatcherChangeTypes.Changed)
                        {
                            waitReport.Set();
                        }
                    };
                    fileWatcher.EnableRaisingEvents = true;

                    Exec("docker-compose", $"{dockerComposeFileArgument} up --build -d", envs);

                    if (waitReport.WaitOne(maxTimeOutSeconds * 1000))
                    {
                        var status = int.Parse(File.ReadAllText(statusFile).Trim());
                        if (status != 0)
                        {
                            throw new ApplicationException($"wait compose proxy status return non-zero code {status}.");
                        }
                    }
                    else
                    {
                        throw new ApplicationException($"time out to get response from wait compose proxy.");
                    }
                }
            }
            return new DockerComposeGroup(tempFolder, actualDockerComposeFile, waitComposeFile);
            string GetDockerComposeVersionFromMainFile()
            {
                var versionLine = File.ReadAllLines(actualDockerComposeFile).FirstOrDefault(p => p.StartsWith("version:"));
                if (versionLine != null)
                {
                    return versionLine.Substring(8).Trim().Replace("\"", string.Empty).Replace("'", string.Empty);
                }
                return "1";
            }

            void GeneratorWaitComposeFile()
            {

                var waithosts = waitingContainerPorts;
                var version = GetDockerComposeVersionFromMainFile();
                var content = $@"version: '{version}'
services:
  wait-compose-ready:
    image: ysknife/wait-compose-ready
    volumes:
    - {statusFile}:/status.txt
    environment:
      WAIT_HOSTS: {waithosts}
      WAIT_TIMEOUT: {maxTimeOutSeconds}
";
                File.WriteAllText(waitComposeFile, content);
            }

            string FindDockerComposeFile()

            {

                var current = Environment.CurrentDirectory;
                var root = Path.GetPathRoot(current);
                while (true)
                {
                    var full = Path.Combine(current, dockerComposeFile);

                    if (File.Exists(full))
                    {
                        return full;
                    }
                    else if (current == root)
                    {
                        return null;
                    }
                    else
                    {
                        current = Path.GetDirectoryName(current);
                    }
                }

            }

        }
        public static IDisposable Up(string dockerComposeFile, IDictionary<string, object> envs, IDictionary<string, int> waitingContainerPorts, int maxTimeOutSeconds = 120)
        {
            return Up(dockerComposeFile, envs, string.Join(", ", waitingContainerPorts?.Select(p => $"{p.Key}:{p.Value}")), maxTimeOutSeconds);
        }
        public static IDisposable Up(IDictionary<string, object> envs, IDictionary<string, int> waitingContainerPorts, int maxTimeOutSeconds = 120)
        {
            return Up("docker-compose.yml", envs, waitingContainerPorts, maxTimeOutSeconds);
        }
        public static IDisposable Up(IDictionary<string, object> envs, string waitingContainerPorts, int maxTimeOutSeconds = 120)
        {
            return Up("docker-compose.yml", envs, waitingContainerPorts, maxTimeOutSeconds);
        }




        public static void Down()
        {
            Exec("docker-compose", "down", null);
        }
        private static int Exec(string fileName, string arguments, IDictionary<string, object> envs)
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
                    var timeout = MaxTimeOutSeconds * 1000;
                    if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))
                    {
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Exec process exit with code: {process.ExitCode}, output: {stringBuilder}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Exec process timeout, total seconds > {MaxTimeOutSeconds}s, output: {stringBuilder}");
                    }
                }
                return process.ExitCode;
            }

        }

        class DockerComposeGroup : IDisposable
        {
            public DockerComposeGroup(string tempFolder, params string[] composeFiles)
            {
                TempFolder = tempFolder;
                ComposeFiles = composeFiles;
            }

            public string TempFolder { get; }
            public string[] ComposeFiles { get; }

            public void Dispose()
            {
                if (ComposeFiles.Length == 0)
                {
                    Exec("docker-compose", "down", null);
                }
                else
                {
                    var dockerComposeFileArgument = string.Join(" ", ComposeFiles.Select(p => $"-f \"{Path.GetFullPath(p)}\""));
                    Exec("docker-compose", $"{dockerComposeFileArgument} down", null);
                }
                if (string.IsNullOrEmpty(TempFolder))
                {
                    DeleteFolder(TempFolder);
                }

            }

            private void DeleteFolder(string tempFolder)
            {
                foreach (var file in Directory.GetFiles(tempFolder))
                {
                    File.Delete(file);
                }
                foreach (var folder in Directory.GetDirectories(tempFolder))
                {
                    DeleteFolder(folder);
                }
                Directory.Delete(tempFolder);
            }
        }
    }
}
