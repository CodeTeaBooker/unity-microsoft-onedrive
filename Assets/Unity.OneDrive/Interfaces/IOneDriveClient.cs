using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Identity.Client;
using Microsoft.Graph;

namespace Unity.OneDrive.Interfaces
{
    /// <summary>
    /// OneDrive client main interface - Directly uses Microsoft.Graph types
    /// </summary>
    public interface IOneDriveClient
    {
        /// <summary>
        /// Whether authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Current user account - Directly uses MSAL's IAccount
        /// </summary>
        Maybe<IAccount> CurrentAccount { get; }

        /// <summary>
        /// Graph service client - Directly exposes Microsoft.Graph.GraphServiceClient
        /// </summary>
        Maybe<GraphServiceClient> GraphClient { get; }

        // <summary>
        /// Initialize client
        /// </summary>
        Task<Result> InitializeAsync();

        /// <summary>
        /// Quick authentication (automated process)
        /// </summary>
        Task<Result> QuickAuthenticateAsync();

        /// <summary>
        /// Full control authentication process - Directly uses MSAL's DeviceCodeResult
        /// </summary>
        Task<Result> AuthenticateAsync(
            Maybe<Action<DeviceCodeResult>> onCodeReady = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sign out
        /// </summary>
        Task<Result> SignOutAsync();
    }
}
