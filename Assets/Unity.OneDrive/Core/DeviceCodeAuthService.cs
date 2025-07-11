using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Identity.Client;
using Unity.OneDrive.Interfaces;
using UnityEngine;

namespace Unity.OneDrive.Core
{
    /// <summary>
    /// Device Code authentication service - Simplified version, based on official solution
    /// </summary>
    public class DeviceCodeAuthService : IAuthenticationService
    {
        private readonly OneDriveConfig _config;
        private readonly IOneDriveLogger _logger;
        private readonly IClipboardHelper _clipboardHelper;
        private readonly IBrowserHelper _browserHelper;

        private static SynchronizationContext _mainThreadContext;

        private IPublicClientApplication _msalApp;
        private bool _isInitialized = false;
        private bool _isAuthenticated = false;


        public bool IsInitialized => _isInitialized;
        public bool IsAuthenticated => _isAuthenticated;

        public event Action<bool> AuthenticationStatusChanged;

        public DeviceCodeAuthService(OneDriveConfig config, IOneDriveLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clipboardHelper = new ClipboardHelper(logger);
            _browserHelper = new BrowserHelper(logger);

            if (_mainThreadContext != null)
            {
                ClipboardHelper.SetMainThreadContext(_mainThreadContext);
                BrowserHelper.SetMainThreadContext(_mainThreadContext);
            }
        }

        public static void InitializeMainThreadContext(SynchronizationContext context)
        {
            _mainThreadContext = context;

            ClipboardHelper.SetMainThreadContext(context);
            BrowserHelper.SetMainThreadContext(context);
        }

        public Task<Result> InitializeAsync()
        {
            try
            {
                _logger.LogInfo("[DeviceCodeAuthService] Initializing MSAL...");

                _msalApp = PublicClientApplicationBuilder
                    .Create(_config.ClientId)
                    .WithAuthority(OneDriveConstants.MICROSOFT_AUTHORITY_URL)
                    .WithRedirectUri(OneDriveConstants.NATIVE_CLIENT_REDIRECT_URI)
                    .Build();

                // Unity PlayerPrefs cache
                _msalApp.UserTokenCache.SetBeforeAccess(BeforeAccessNotification);
                _msalApp.UserTokenCache.SetAfterAccess(AfterAccessNotification);

                _isInitialized = true;
                _logger.LogInfo("[DeviceCodeAuthService] Initialization successful");
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Initialization failed");
                return Task.FromResult(Result.Failure(ex.Message));
            }
        }

        public async Task<Result<AuthenticationResult>> TryGetCachedTokenAsync()
        {
            try
            {
                if (!_isInitialized)
                    return Result.Failure<AuthenticationResult>("Not initialized");

                var accounts = await _msalApp.GetAccountsAsync();
                var account = accounts.FirstOrDefault();

                if (account == null)
                    return Result.Failure<AuthenticationResult>("No cached account");

                var result = await _msalApp.AcquireTokenSilent(_config.Scopes, account).ExecuteAsync();

                _isAuthenticated = true;
                OnAuthenticationStatusChanged(true);
                _logger.LogInfo($"[DeviceCodeAuthService] Cached authentication successful: {result.Account.Username}");

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Cached authentication failed");
                return Result.Failure<AuthenticationResult>(ex.Message);
            }
        }

        public async Task<Result<AuthenticationResult>> AuthenticateWithDeviceCodeAsync(
            bool automate = true,
            Maybe<Action<DeviceCodeResult>> onCodeReady = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_isInitialized)
                    return Result.Failure<AuthenticationResult>("Not initialized");

                _logger.LogInfo("[DeviceCodeAuthService] Starting Device Code Flow...");

                var result = await _msalApp.AcquireTokenWithDeviceCode(_config.Scopes, async deviceCodeResult =>
                {
                    if (automate)
                        await PerformAutomation(deviceCodeResult);

                    onCodeReady.Execute(callback => callback(deviceCodeResult));

                }).ExecuteAsync(cancellationToken);

                _isAuthenticated = true;
                OnAuthenticationStatusChanged(true);
                _logger.LogInfo($"[DeviceCodeAuthService] Authentication successful: {result.Account.Username}");

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Authentication failed");
                return Result.Failure<AuthenticationResult>(ex.Message);
            }
        }

        public async Task<Maybe<IAccount>> GetCurrentAccountAsync()
        {
            try
            {
                if (!_isInitialized) return Maybe<IAccount>.None;

                var accounts = await _msalApp.GetAccountsAsync();
                var account = accounts.FirstOrDefault();
                return account != null ? Maybe<IAccount>.From(account) : Maybe<IAccount>.None;
            }
            catch
            {
                return Maybe<IAccount>.None;
            }
        }



        public async Task<Result> SignOutAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    var accounts = await _msalApp.GetAccountsAsync();
                    foreach (var account in accounts)
                    {
                        await _msalApp.RemoveAsync(account);
                    }
                }

                PlayerPrefs.DeleteKey("Unity.OneDrive.TokenCache");
                PlayerPrefs.Save();

                _isAuthenticated = false;
                OnAuthenticationStatusChanged(false);
                _logger.LogInfo("[DeviceCodeAuthService] Sign out successful");

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private async Task PerformAutomation(DeviceCodeResult deviceCodeResult)
        {
            try
            {
                var tasks = new List<Task>();

                if (_config.AutoCopyToClipboard && _clipboardHelper.IsClipboardAvailable)
                    tasks.Add(_clipboardHelper.CopyToClipboardAsync(deviceCodeResult.UserCode));

                if (_config.AutoOpenBrowser && _browserHelper.CanOpenBrowser)
                    tasks.Add(_browserHelper.OpenBrowserAsync(deviceCodeResult.VerificationUrl));

                await Task.WhenAll(tasks);
                _logger.LogInfo($"[DeviceCodeAuthService] Automation completed! Code: {deviceCodeResult.UserCode}");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Automation failed");
            }
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            try
            {
                string tokenCache = null;

                if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                {
                    tokenCache = PlayerPrefs.GetString("Unity.OneDrive.TokenCache", "");
                }
                else if (_mainThreadContext != null)
                {
                    var resetEvent = new ManualResetEventSlim(false);
                    string capturedToken = null;

                    _mainThreadContext.Post(_ =>
                    {
                        capturedToken = PlayerPrefs.GetString("Unity.OneDrive.TokenCache", "");
                        resetEvent.Set();
                    }, null);

                    if (resetEvent.Wait(TimeSpan.FromSeconds(5)))
                    {
                        tokenCache = capturedToken;
                    }
                    else
                    {
                        _logger.LogWarning("[DeviceCodeAuthService] Timeout reading token from PlayerPrefs");
                    }
                }

                if (!string.IsNullOrEmpty(tokenCache))
                {
                    var tokenData = Convert.FromBase64String(tokenCache);
                    args.TokenCache.DeserializeMsalV3(tokenData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Token loading failed");
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                try
                {
                    var tokenData = args.TokenCache.SerializeMsalV3();
                    var base64Token = Convert.ToBase64String(tokenData);

                    if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                    {

                        SaveTokenToPlayerPrefs(base64Token);
                    }
                    else if (_mainThreadContext != null)
                    {

                        _mainThreadContext.Post(_ =>
                        {
                            SaveTokenToPlayerPrefs(base64Token);
                        }, null);
                    }
                    else
                    {

                        _logger.LogWarning("[DeviceCodeAuthService] No SynchronizationContext available, token not saved");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Token saving failed");
                }
            }
        }



        private void SaveTokenToPlayerPrefs(string base64Token)
        {
            try
            {
                PlayerPrefs.SetString("Unity.OneDrive.TokenCache", base64Token);
                PlayerPrefs.Save();
                _logger.LogInfo("[DeviceCodeAuthService] Token saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to save token to PlayerPrefs");
            }
        }


        private void OnAuthenticationStatusChanged(bool isAuthenticated)
        {
            try
            {
                AuthenticationStatusChanged?.Invoke(isAuthenticated);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Event handling failed");
            }
        }
    }
}