using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CSharpFunctionalExtensions;
using Unity.OneDrive.Interfaces;

namespace Unity.OneDrive.Core
{
    public class BrowserHelper : IBrowserHelper
    {
        private readonly IOneDriveLogger _logger;
        private static bool? _cachedIsPlaying;
        private static readonly object _lock = new object();
        private static SynchronizationContext _mainThreadContext;

        public BrowserHelper(IOneDriveLogger logger)
        {
            _logger = logger ?? new UnityLogger();
        }

        public bool CanOpenBrowser
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

        public async Task<Result<TimeSpan>> OpenBrowserAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return Result.Failure<TimeSpan>("URL cannot be empty");

            if (!CanOpenBrowser)
                return Result.Failure<TimeSpan>("Browser not available");

            try
            {
                var uri = new Uri(url);
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    return Result.Failure<TimeSpan>("Invalid URL format");

                var startTime = DateTime.UtcNow;
                
                try
                {
                   
                    if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                    {
                       
                        Application.OpenURL(url);
                        _logger.LogInfo($"[BrowserHelper] Browser opened successfully");
                    }
                    else if (_mainThreadContext != null)
                    {
                        
                        var resetEvent = new ManualResetEventSlim(false);
                        Exception capturedException = null;

                        _mainThreadContext.Post(_ =>
                        {
                            try
                            {
                                Application.OpenURL(url);
                                _logger.LogInfo($"[BrowserHelper] Browser opened successfully (via SynchronizationContext)");
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
                            _logger.LogWarning($"[BrowserHelper] Timeout opening browser via SynchronizationContext");
                            _logger.LogInfo($"[BrowserHelper] Please open this URL manually: {url}");
                        }
                    }
                    else
                    {
                        
                        _logger.LogInfo($"[BrowserHelper] Cannot open browser from background thread");
                        _logger.LogInfo($"[BrowserHelper] Please open this URL manually: {url}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[BrowserHelper] Browser open failed: {ex.Message}");
                    _logger.LogInfo($"[BrowserHelper] Please open this URL manually: {url}");
                }

                await Task.Delay(50);
                var duration = DateTime.UtcNow - startTime;
                
                return Result.Success(duration);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Browser opening failed");
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