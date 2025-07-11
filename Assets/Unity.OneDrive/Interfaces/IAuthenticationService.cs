using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Identity.Client;

namespace Unity.OneDrive.Interfaces
{
    /// <summary>
    /// Authentication service interface - Simplified version
    /// </summary>
    public interface IAuthenticationService
    {
        bool IsInitialized { get; }
        bool IsAuthenticated { get; }

        Task<Result> InitializeAsync();
        Task<Maybe<IAccount>> GetCurrentAccountAsync();
        Task<Result<AuthenticationResult>> TryGetCachedTokenAsync();
        Task<Result<AuthenticationResult>> AuthenticateWithDeviceCodeAsync(
            bool automate = true,
            Maybe<Action<DeviceCodeResult>> onCodeReady = default,
            CancellationToken cancellationToken = default);
        Task<Result> SignOutAsync();

        event Action<bool> AuthenticationStatusChanged;
    }
}
