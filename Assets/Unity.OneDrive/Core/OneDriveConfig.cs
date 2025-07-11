using System;
using CSharpFunctionalExtensions;

namespace Unity.OneDrive.Core
{
    public class OneDriveConfig
    {
        public string ClientId { get; }
        public string[] Scopes { get; }
        public bool AutoCopyToClipboard { get; }
        public bool AutoOpenBrowser { get; }
        public bool EnableDetailedLogging { get; }

        public OneDriveConfig(
            string clientId,
            bool enableDetailedLogging = false,
            bool autoCopyToClipboard = true,
            bool autoOpenBrowser = true)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Scopes = new[] { "Files.ReadWrite.All", "User.Read" };
            EnableDetailedLogging = enableDetailedLogging;
            AutoCopyToClipboard = autoCopyToClipboard;
            AutoOpenBrowser = autoOpenBrowser;
        }

        public Result ValidateConfiguration() =>
            string.IsNullOrEmpty(ClientId)
                ? Result.Failure("ClientId cannot be empty")
                : Result.Success();
    }
}