using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Unity.OneDrive.Interfaces
{
     /// <summary>
    /// Platform tool interfaces - Keep unchanged
    /// </summary>
    public interface IClipboardHelper
    {
        Task<Result<TimeSpan>> CopyToClipboardAsync(string text);
        bool IsClipboardAvailable { get; }
    }
}
