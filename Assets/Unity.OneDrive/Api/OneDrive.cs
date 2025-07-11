using Unity.OneDrive.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using CSharpFunctionalExtensions;
using Unity.OneDrive.Interfaces;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Unity.OneDrive.Api
{
    /// <summary>
    /// Unity OneDrive SDK Public API - Simplified Version
    /// </summary>
    public static class OneDrive
    {
        private static Maybe<IOneDriveClient> _client = Maybe<IOneDriveClient>.None;

        public static bool IsInitialized => _client.HasValue;
        public static bool IsAuthenticated => _client.Map(c => c.IsAuthenticated).GetValueOrDefault(false);
        public static Maybe<IAccount> CurrentAccount => _client.Bind(c => c.CurrentAccount);
        public static Maybe<GraphServiceClient> GraphClient => _client.Bind(c => c.GraphClient);

        public static async Task<Result> InitializeAsync(
            string clientId,
            bool enableDetailedLogging = false,
            bool autoCopyToClipboard = true,
            bool autoOpenBrowser = true)
        {
            try
            {   
                var mainThreadContext = SynchronizationContext.Current;
                if (mainThreadContext != null)
                {
                    DeviceCodeAuthService.InitializeMainThreadContext(mainThreadContext);
                }
                else
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        UnityEngine.Debug.LogWarning("[OneDrive] No SynchronizationContext found, automation features may not work properly");
                    }
                }

                ClipboardHelper.Initialize();
                BrowserHelper.Initialize();

                var config = new OneDriveConfig(clientId, enableDetailedLogging, autoCopyToClipboard, autoOpenBrowser);
                var logger = new UnityLogger(enableDetailedLogging);
                var authService = new DeviceCodeAuthService(config, logger);
                _client = new OneDriveClient(config, authService, logger);

                return await _client.Value.InitializeAsync();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public static async Task<Result> QuickAuthenticateAsync()
        {
            if (!_client.HasValue)
                return Result.Failure("SDK not initialized");

            return await _client.Value.QuickAuthenticateAsync();
        }

        public static async Task<Result> AuthenticateAsync(
            Maybe<Action<DeviceCodeResult>> onCodeReady = default,
            CancellationToken cancellationToken = default)
        {
            if (!_client.HasValue)
                return Result.Failure("SDK not initialized");

            return await _client.Value.AuthenticateAsync(onCodeReady, cancellationToken);
        }

        public static async Task<Result> SignOutAsync()
        {
            if (!_client.HasValue)
                return Result.Failure("SDK not initialized");

            return await _client.Value.SignOutAsync();
        }

        public static void Dispose()
        {
            _client.Execute(client =>
            {
                if (client is IDisposable disposable)
                    disposable.Dispose();
            });
            _client = Maybe<IOneDriveClient>.None;
        }

        // Graph API shortcut methods
        public static async Task<Result<DriveItemCollectionResponse>> GetFilesAsync(string folderId = null)
        {
            try
            {
                if (!_client.HasValue || !_client.Value.GraphClient.HasValue)
                    return Result.Failure<DriveItemCollectionResponse>("Not authenticated");

                var graphClient = _client.Value.GraphClient.Value;
                var drive = await graphClient.Me.Drive.GetAsync();

                if (drive?.Id == null)
                    return Result.Failure<DriveItemCollectionResponse>("Unable to get Drive");

                var result = string.IsNullOrEmpty(folderId)
                    ? await graphClient.Drives[drive.Id].Items["root"].Children.GetAsync()
                    : await graphClient.Drives[drive.Id].Items[folderId].Children.GetAsync();

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error occurred" : ex.Message;
                return Result.Failure<DriveItemCollectionResponse>(errorMessage);
            }
        }

        public static async Task<Result<User>> GetUserAsync()
        {
            try
            {
                if (!_client.HasValue || !_client.Value.GraphClient.HasValue)
                    return Result.Failure<User>("Not authenticated");

                var user = await _client.Value.GraphClient.Value.Me.GetAsync();
                return Result.Success(user);
            }
            catch (Exception ex)
            {
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error occurred" : ex.Message;
                return Result.Failure<User>(errorMessage);
            }
        }

        public static async Task<Result<Drive>> GetDriveAsync()
        {
            try
            {
                if (!_client.HasValue || !_client.Value.GraphClient.HasValue)
                    return Result.Failure<Drive>("Not authenticated");

                var drive = await _client.Value.GraphClient.Value.Me.Drive.GetAsync();
                return Result.Success(drive);
            }
            catch (Exception ex)
            {
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Unknown error occurred" : ex.Message;
                return Result.Failure<Drive>(errorMessage);
            }
        }
    }
}