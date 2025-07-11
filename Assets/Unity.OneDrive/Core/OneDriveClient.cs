using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Unity.OneDrive.Interfaces;
using Microsoft.Graph.Models;

namespace Unity.OneDrive.Core
{
    /// <summary>
    /// OneDrive client - Microsoft.Graph 5.84.0 fully compatible version
    /// Integrates Kiota authentication provider and MSAL.NET
    /// </summary>
    public class OneDriveClient : IOneDriveClient, IDisposable
    {
        private readonly OneDriveConfig _config;
        private readonly IAuthenticationService _authService;
        private readonly IOneDriveLogger _logger;

        private bool _isInitialized = false;
        private Maybe<IAccount> _currentAccount = Maybe<IAccount>.None;
        private Maybe<GraphServiceClient> _graphClient = Maybe<GraphServiceClient>.None;
        private MsalIntegratedAuthProvider _authProvider;

        public bool IsAuthenticated => _authService?.IsAuthenticated ?? false;
        public Maybe<IAccount> CurrentAccount => _currentAccount;
        public Maybe<GraphServiceClient> GraphClient => _graphClient;

        public OneDriveClient(OneDriveConfig config, IAuthenticationService authService, IOneDriveLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? new UnityLogger();

            _authService.AuthenticationStatusChanged += OnAuthenticationStatusChanged;
            _logger.LogInfo("[OneDriveClient] Client created (Graph 5.84.0 compatible version)");
        }

        public async Task<Result> InitializeAsync()
        {
            try
            {
                _logger.LogInfo("[OneDriveClient] Initializing client (Graph 5.84.0)...");

                if (string.IsNullOrEmpty(_config.ClientId))
                    return Result.Failure("ClientId cannot be empty");

                var authResult = await _authService.InitializeAsync();
                if (authResult.IsFailure)
                    return authResult;

                var checkResult = await CheckExistingAuthentication();
                if (checkResult.IsFailure)
                    return checkResult;

                _isInitialized = true;
                _logger.LogInfo("[OneDriveClient] Initialization successful (Graph 5.84.0 compatible)");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Initialization failed");
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result> QuickAuthenticateAsync()
        {
            try
            {
                if (!_isInitialized)
                    return Result.Failure("Not initialized");

                if (IsAuthenticated)
                    return Result.Success();

                var authResult = await PerformAuthentication();
                return authResult;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Quick authentication failed");
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result> AuthenticateAsync(
            Maybe<Action<DeviceCodeResult>> onCodeReady = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_isInitialized)
                    return Result.Failure("Not initialized");

                var authResult = await _authService.AuthenticateWithDeviceCodeAsync(
                    _config.AutoCopyToClipboard || _config.AutoOpenBrowser,
                    onCodeReady,
                    cancellationToken);

                if (authResult.IsFailure)
                    return Result.Failure(authResult.Error);

                var graphResult = await CreateGraphClient(authResult.Value);
                return graphResult;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Authentication failed");
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result> SignOutAsync()
        {
            try
            {
                var result = await _authService.SignOutAsync();
                if (result.IsSuccess)
                {
                    _currentAccount = Maybe<IAccount>.None;
                    _graphClient = Maybe<GraphServiceClient>.None;

                    // Clean up authentication provider
                    _authProvider?.Dispose();
                    _authProvider = null;
                }
                return result;
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private async Task<Result> CheckExistingAuthentication()
        {
            try
            {
                var cachedResult = await _authService.TryGetCachedTokenAsync();
                if (cachedResult.IsSuccess)
                {
                    return await CreateGraphClient(cachedResult.Value);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private async Task<Result> PerformAuthentication()
        {
            try
            {
                var silentResult = await _authService.TryGetCachedTokenAsync();
                if (silentResult.IsSuccess)
                {
                    return await CreateGraphClient(silentResult.Value);
                }

                var authResult = await _authService.AuthenticateWithDeviceCodeAsync(automate: true);
                if (authResult.IsSuccess)
                {
                    return await CreateGraphClient(authResult.Value);
                }

                return Result.Failure(authResult.Error);
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Create GraphServiceClient - Microsoft.Graph 5.84.0 + Kiota fully compatible solution
        /// </summary>
        private Task<Result> CreateGraphClient(AuthenticationResult authResult)
        {
            try
            {
                _logger.LogInfo("[OneDriveClient] Creating GraphServiceClient (Kiota integration)...");

                // Clean up old authentication provider
                _authProvider?.Dispose();

                // Create new MsalIntegratedAuthProvider with MSAL.NET
                _authProvider = new MsalIntegratedAuthProvider(_authService, _config.Scopes);

                // Create GraphServiceClient - Kiota compatible way
                var graphClient = new GraphServiceClient(_authProvider);

                _graphClient = graphClient;
                _currentAccount = Maybe<IAccount>.From(authResult.Account);

                _logger.LogInfo("[OneDriveClient] GraphServiceClient created successfully (Kiota integration)");
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "GraphServiceClient creation failed");
                return Task.FromResult(Result.Failure(ex.Message));
            }
        }

        /// <summary>
        /// Validate Graph API connection
        /// </summary>
        public async Task<Result> ValidateConnectionAsync()
        {
            try
            {
                if (!_graphClient.HasValue)
                    return Result.Failure("GraphServiceClient not initialized");

                // Try calling a simple API to validate connection
                var user = await _graphClient.Value.Me.GetAsync();

                _logger.LogInfo($"[OneDriveClient] Connection validation successful: {user.DisplayName}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Connection validation failed");
                return Result.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Get Graph API client information
        /// </summary>
        public string GetClientInfo()
        {
            return $"Microsoft.Graph 5.84.0 + Kiota Integration + MSAL.NET 4.73.1";
        }

        private void OnAuthenticationStatusChanged(bool isAuthenticated)
        {
            if (!isAuthenticated)
            {
                _currentAccount = Maybe<IAccount>.None;
                _graphClient = Maybe<GraphServiceClient>.None;
                _authProvider?.Dispose();
                _authProvider = null;
            }
        }

        public void Dispose()
        {
            _authService.AuthenticationStatusChanged -= OnAuthenticationStatusChanged;
            _authProvider?.Dispose();
            _graphClient.Execute(client => client?.Dispose());
        }
    }

    /// <summary>
    /// Extended OneDrive client - Add more Graph API functionality
    /// </summary>
    public static class OneDriveClientExtensions
    {
        /// <summary>
        /// Get user information - Using Graph 5.84.0 syntax
        /// </summary>
        public static async Task<Result<User>> GetUserInfoAsync(this IOneDriveClient client)
        {
            try
            {
                if (!client.GraphClient.HasValue)
                    return Result.Failure<User>("Not authenticated");

                var user = await client.GraphClient.Value.Me.GetAsync();
                return Result.Success(user);
            }
            catch (Exception ex)
            {
                return Result.Failure<User>(ex.Message);
            }
        }

        /// <summary>
        /// Get drive information - Using Graph 5.84.0 syntax
        /// </summary>
        public static async Task<Result<Drive>> GetDriveInfoAsync(this IOneDriveClient client)
        {
            try
            {
                if (!client.GraphClient.HasValue)
                    return Result.Failure<Drive>("Not authenticated");

                var drive = await client.GraphClient.Value.Me.Drive.GetAsync();
                return Result.Success(drive);
            }
            catch (Exception ex)
            {
                return Result.Failure<Drive>(ex.Message);
            }
        }

        /// <summary>
        /// Get file list - Using Graph 5.84.0 syntax
        /// </summary>
        public static async Task<Result<DriveItemCollectionResponse>> GetFilesAsync(
            this IOneDriveClient client,
            string folderId = null)
        {
            try
            {
                if (!client.GraphClient.HasValue)
                    return Result.Failure<DriveItemCollectionResponse>("Not authenticated");

                var graphClient = client.GraphClient.Value;
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
                return Result.Failure<DriveItemCollectionResponse>(ex.Message);
            }
        }

        /// <summary>
        /// Upload file - Using Graph 5.84.0 syntax
        /// </summary>
        public static async Task<Result<DriveItem>> UploadFileAsync(
            this IOneDriveClient client,
            string fileName,
            byte[] fileContent)
        {
            try
            {
                if (!client.GraphClient.HasValue)
                    return Result.Failure<DriveItem>("Not authenticated");

                var graphClient = client.GraphClient.Value;
                var drive = await graphClient.Me.Drive.GetAsync();

                if (drive?.Id == null)
                    return Result.Failure<DriveItem>("Unable to get Drive");

                using var stream = new System.IO.MemoryStream(fileContent);
                var uploadedItem = await graphClient.Drives[drive.Id]
                    .Items["root"]
                    .ItemWithPath(fileName)
                    .Content
                    .PutAsync(stream);

                return Result.Success(uploadedItem);
            }
            catch (Exception ex)
            {
                return Result.Failure<DriveItem>(ex.Message);
            }
        }
    }
}