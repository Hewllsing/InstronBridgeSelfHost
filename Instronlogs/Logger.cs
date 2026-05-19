using System;
using System.IO;

namespace InstronBridgeSelfHost.InstronLogs
{
    public static class Logger
    {
        private static readonly object LockObject = new object();

        private static string LogFile
        {
            get
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                return Path.Combine(
                    baseDirectory,
                    "InstronLogs",
                    "logs.txt"
                );
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            var fullMessage = ex == null
                ? message
                : message + " | " + ex.Message + " | " + ex.StackTrace;

            Write("ERROR", fullMessage);
        }

        private static void Write(string level, string message)
        {
            lock (LockObject)
            {
                var directory = Path.GetDirectoryName(LogFile);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
        }
    }
}