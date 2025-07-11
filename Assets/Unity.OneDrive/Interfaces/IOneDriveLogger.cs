using System;

namespace Unity.OneDrive.Interfaces
{
    public interface IOneDriveLogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogException(Exception exception, string message = null);
    }
}