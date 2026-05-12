using System;
using System.IO;
using System.Text;

namespace PredareAmef.Services
{
    public enum LogLevel { Info, Success, Warning, Error, Debug }

    public interface ILogger
    {
        void Log(string message, LogLevel level = LogLevel.Info);
        void Progress(int stepIndex, int totalSteps, string stepName);
        /// <summary>Progres fin in cadrul unui pas: current/total cu label (KB citit, Z procesate etc.).</summary>
        void SubProgress(long current, long total, string label);
    }

    public sealed class ActionLogger : ILogger, IDisposable
    {
        private readonly Action<string, LogLevel> _log;
        private readonly Action<int, int, string> _progress;
        private readonly Action<long, long, string> _subProgress;
        private readonly object _fileLock = new object();
        private StreamWriter _fileWriter;
        private string _filePath;

        public ActionLogger(Action<string, LogLevel> log,
                            Action<int, int, string> progress = null,
                            Action<long, long, string> subProgress = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _progress = progress;
            _subProgress = subProgress;
        }

        /// <summary>
        /// Ataseaza un fisier de log. Toate apelurile Log() ulterioare vor scrie si in fisier
        /// (append, UTF-8, flush la fiecare linie). Apelat dupa crearea folderului output.
        /// </summary>
        public void AttachFile(string path)
        {
            lock (_fileLock)
            {
                try
                {
                    DetachFileLocked();
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _fileWriter = new StreamWriter(path, append: true, encoding: new UTF8Encoding(false))
                    {
                        AutoFlush = true
                    };
                    _filePath = path;
                    _fileWriter.WriteLine("=== Sesiune predare " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
                }
                catch
                {
                    DetachFileLocked();
                }
            }
        }

        public string LogFilePath
        {
            get { lock (_fileLock) return _filePath; }
        }

        private void DetachFileLocked()
        {
            try { _fileWriter?.Dispose(); } catch { }
            _fileWriter = null;
            _filePath = null;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            _log(message, level);

            lock (_fileLock)
            {
                if (_fileWriter == null) return;
                try
                {
                    string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + level.ToString().ToUpperInvariant().PadRight(7) + " " + message;
                    _fileWriter.WriteLine(line);
                }
                catch { }
            }
        }

        public void Progress(int stepIndex, int totalSteps, string stepName)
        {
            _progress?.Invoke(stepIndex, totalSteps, stepName);

            lock (_fileLock)
            {
                if (_fileWriter == null) return;
                try
                {
                    _fileWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] >>> Pas " + stepIndex + "/" + totalSteps + ": " + stepName);
                }
                catch { }
            }
        }

        public void SubProgress(long current, long total, string label)
        {
            _subProgress?.Invoke(current, total, label);
            // Sub-progress NU se scrie in fisier (s-ar polua cu sute de linii per secunda)
        }

        public void Dispose()
        {
            lock (_fileLock) DetachFileLocked();
        }
    }
}
