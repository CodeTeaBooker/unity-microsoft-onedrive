using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Unity.OneDrive.Interfaces;

namespace Unity.OneDrive.Core
{
    /// <summary>
    /// Microsoft.Graph 5.84.0 compatible access token authentication provider
    /// Based on Microsoft.Kiota.Abstractions.Authentication.IAuthenticationProvider
    /// </summary>
    public class AccessTokenAuthProvider : IAuthenticationProvider
    {
        private readonly string _accessToken;
        private readonly DateTimeOffset _expiresOn;

        public AccessTokenAuthProvider(string accessToken, DateTimeOffset? expiresOn = null)
        {
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _expiresOn = expiresOn ?? DateTimeOffset.UtcNow.AddHours(1);
        }

        /// <summary>
        /// Authenticate request - Kiota abstraction layer interface implementation
        /// </summary>
        /// <param name="request">Request information</param>
        /// <param name="additionalAuthenticationContext">Additional authentication context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication task</returns>
        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object> additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Check if token is expired
            if (DateTimeOffset.UtcNow >= _expiresOn)
            {
                throw new InvalidOperationException("Access token has expired, re-authentication required");
            }

            // Add Authorization header - complies with Kiota requirements
            request.Headers.TryAdd("Authorization", $"Bearer {_accessToken}");

            // Kiota requires async completion
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Enhanced access token authentication provider
    /// Supports token refresh and automatic retry
    /// </summary>
    public class EnhancedAccessTokenAuthProvider : IAuthenticationProvider
    {
        private readonly Func<Task<string>> _tokenRefreshFunc;
        private string _currentToken;
        private DateTimeOffset _expiresOn;
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

        public EnhancedAccessTokenAuthProvider(
            string initialToken,
            DateTimeOffset expiresOn,
            Func<Task<string>> tokenRefreshFunc)
        {
            _currentToken = initialToken ?? throw new ArgumentNullException(nameof(initialToken));
            _expiresOn = expiresOn;
            _tokenRefreshFunc = tokenRefreshFunc ?? throw new ArgumentNullException(nameof(tokenRefreshFunc));
        }

        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object> additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Check and refresh token
            await EnsureValidTokenAsync(cancellationToken);

            // Set authentication header
            request.Headers.TryAdd("Authorization", $"Bearer {_currentToken}");
        }

        private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
        {
            // If token is about to expire (refresh 5 minutes early)
            if (DateTimeOffset.UtcNow.AddMinutes(5) >= _expiresOn)
            {
                await _refreshSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Double-checked locking pattern
                    if (DateTimeOffset.UtcNow.AddMinutes(5) >= _expiresOn)
                    {
                        _currentToken = await _tokenRefreshFunc();
                        _expiresOn = DateTimeOffset.UtcNow.AddHours(1); // Assume new token expires in 1 hour
                    }
                }
                finally
                {
                    _refreshSemaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            _refreshSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// MSAL integrated authentication provider
    /// Directly integrates with MSAL.NET, supports automatic token refresh
    /// </summary>
    public class MsalIntegratedAuthProvider : IAuthenticationProvider, IDisposable
    {
        private readonly IAuthenticationService _authService;
        private readonly string[] _scopes;
        private AuthenticationResult _lastResult;
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

        public MsalIntegratedAuthProvider(IAuthenticationService authService, string[] scopes)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        }

        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object> additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var token = await GetValidTokenAsync(cancellationToken);
            request.Headers.TryAdd("Authorization", $"Bearer {token}");
        }

        private async Task<string> GetValidTokenAsync(CancellationToken cancellationToken)
        {
            // If no token or token is about to expire
            if (_lastResult == null || 
                DateTimeOffset.UtcNow.AddMinutes(5) >= _lastResult.ExpiresOn)
            {
                await _refreshSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Double check
                    if (_lastResult == null || 
                        DateTimeOffset.UtcNow.AddMinutes(5) >= _lastResult.ExpiresOn)
                    {
                        var tokenResult = await _authService.TryGetCachedTokenAsync();
                        if (tokenResult.IsSuccess)
                        {
                            _lastResult = tokenResult.Value;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unable to get valid token: {tokenResult.Error}");
                        }
                    }
                }
                finally
                {
                    _refreshSemaphore.Release();
                }
            }

            return _lastResult.AccessToken;
        }

        public void Dispose()
        {
            _refreshSemaphore?.Dispose();
        }
    }
}