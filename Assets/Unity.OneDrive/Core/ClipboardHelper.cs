using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CSharpFunctionalExtensions;
using Unity.OneDrive.Interfaces;

namespace Unity.OneDrive.Core
{
    public class ClipboardHelper : IClipboardHelper
    {
        private readonly IOneDriveLogger _logger;
        private static bool? _cachedIsPlaying;
        private static readonly object _lock = new object();
        private static SynchronizationContext _mainThreadContext;

        public ClipboardHelper(IOneDriveLogger logger)
        {
            _logger = logger ?? new UnityLogger();
        }

        public bool IsClipboardAvailable
        {
            get
            {
                lock (_lock)
                {
                    if (_cachedIsPlaying.HasValue)
                        return _cachedIsPlaying.Value;
                    try
                    {
                        if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                        {
                            _cachedIsPlaying = Application.isPlaying;
                            return _cachedIsPlaying.Value;
                        }
                    }
                    catch { }

                    return true;
                }
            }
        }

        public async Task<Result<TimeSpan>> CopyToClipboardAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Result.Failure<TimeSpan>("Text cannot be empty");

            if (!IsClipboardAvailable)
                return Result.Failure<TimeSpan>("Clipboard not available");

            try
            {
                var startTime = DateTime.UtcNow;

                try
                {
                   
                    if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                    {
                       
                        GUIUtility.systemCopyBuffer = text;
                        _logger.LogInfo($"[ClipboardHelper] Text copied to clipboard successfully");
                    }
                    else if (_mainThreadContext != null)
                    {
                       
                        var resetEvent = new ManualResetEventSlim(false);
                        Exception capturedException = null;

                        _mainThreadContext.Post(_ =>
                        {
                            try
                            {
                                GUIUtility.systemCopyBuffer = text;
                                _logger.LogInfo($"[ClipboardHelper] Text copied to clipboard successfully (via SynchronizationContext)");
                            }
                            catch (Exception ex)
                            {
                                capturedException = ex;
                            }
                            finally
                            {
                                resetEvent.Set();
                            }
                        }, null);

                       
                        if (resetEvent.Wait(TimeSpan.FromSeconds(5)))
                        {
                            if (capturedException != null)
                                throw capturedException;
                        }
                        else
                        {
                            _logger.LogWarning($"[ClipboardHelper] Timeout copying to clipboard via SynchronizationContext");
                            _logger.LogInfo($"[ClipboardHelper] Device code: {text}");
                        }
                    }
                    else
                    {
                       
                        _logger.LogInfo($"[ClipboardHelper] Device code: {text}");
                        _logger.LogInfo("[ClipboardHelper] Cannot access clipboard from background thread - please copy manually");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[ClipboardHelper] Clipboard access failed: {ex.Message}");
                    _logger.LogInfo($"[ClipboardHelper] Device code: {text}");
                }

                await Task.Delay(50);
                var duration = DateTime.UtcNow - startTime;

                return Result.Success(duration);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Clipboard copy failed");
                return Result.Failure<TimeSpan>(ex.Message);
            }
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                try
                {
                    if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                    {
                        _cachedIsPlaying = Application.isPlaying;
                      
                        _mainThreadContext = SynchronizationContext.Current;
                    }
                }
                catch { }
            }
        }

        public static void SetMainThreadContext(SynchronizationContext context)
        {
            _mainThreadContext = context;
        }
    }
}