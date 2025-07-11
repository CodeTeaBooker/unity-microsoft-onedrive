using System;
using UnityEngine;
using Unity.OneDrive.Interfaces;

namespace Unity.OneDrive.Core
{
    public class UnityLogger : IOneDriveLogger
    {
        private readonly bool _enableDetailedLogging;
        private readonly string _prefix;

        public UnityLogger(bool enableDetailedLogging = false)
        {
            _enableDetailedLogging = enableDetailedLogging;
            _prefix = "[OneDrive]";
        }

        public void LogInfo(string message) =>
            Debug.Log(FormatMessage("INFO", message));

        public void LogWarning(string message) =>
            Debug.LogWarning(FormatMessage("WARN", message));

        public void LogError(string message) =>
            Debug.LogError(FormatMessage("ERROR", message));

        public void LogException(Exception exception, string message = null)
        {
            var exceptionMessage = string.IsNullOrEmpty(message)
                ? $"Exception: {exception.Message}"
                : $"{message} - Exception: {exception.Message}";

            Debug.LogError(FormatMessage("ERROR", exceptionMessage));

            if (_enableDetailedLogging)
                Debug.LogError($"Stack trace:\n{exception.StackTrace}");
        }

        private string FormatMessage(string level, string message) =>
            _enableDetailedLogging
                ? $"{_prefix} [{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}"
                : $"{_prefix} [{level}] {message}";
    }
}