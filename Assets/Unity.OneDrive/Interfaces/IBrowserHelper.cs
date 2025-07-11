using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Unity.OneDrive.Interfaces
{
    public interface IBrowserHelper
    {
        Task<Result<TimeSpan>> OpenBrowserAsync(string url);
        bool CanOpenBrowser { get; }
    }
}
