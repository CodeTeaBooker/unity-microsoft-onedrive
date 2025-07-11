using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Microsoft.Identity.Client;
using System;

namespace Unity.OneDrive.Android.Tests
{
    /// <summary>
    /// Android OneDrive Basic Function Tester
    /// Designed for testing initialization, authentication process, user info retrieval and file listing functionality
    /// </summary>
    public class AndroidOneDriveBasicTester : MonoBehaviour
    {
        [Header("Azure Application Configuration")]
        [SerializeField] private string clientId = "your-client-id-here";
        
        [Header("UI Components")]
        [SerializeField] private Button initializeButton;
        [SerializeField] private Button authenticateButton;
        [SerializeField] private Button getUserInfoButton;
        [SerializeField] private Button getFilesButton;
        [SerializeField] private Button signOutButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect logScrollRect;
        
        [Header("Test Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isAuthenticated = false;
        
        private StringBuilder logBuilder = new StringBuilder();

        private void Start()
        {
            SetupUI();
            LogMessage("Android OneDrive Basic Function Tester Started");
            LogMessage($"Unity Version: {Application.unityVersion}");
            LogMessage($"Target Frame Rate: {Application.targetFrameRate}");
        }

        private void SetupUI()
        {
            // Bind button events
            if (initializeButton != null)
                initializeButton.onClick.AddListener(() => _ = InitializeSDKAsync());
            
            if (authenticateButton != null)
                authenticateButton.onClick.AddListener(() => _ = AuthenticateAsync());
            
            if (getUserInfoButton != null)
                getUserInfoButton.onClick.AddListener(() => _ = GetUserInfoAsync());
            
            if (getFilesButton != null)
                getFilesButton.onClick.AddListener(() => _ = GetFilesAsync());
            
            if (signOutButton != null)
                signOutButton.onClick.AddListener(() => _ = SignOutAsync());

            // Set initial state
            UpdateButtonStates();
            UpdateStatusText("Waiting for initialization");
        }

        /// <summary>
        /// Test 1: SDK Initialization
        /// </summary>
        public async Task InitializeSDKAsync()
        {
            try
            {
                LogMessage("========== Starting Test: SDK Initialization ==========");
                UpdateStatusText("Initializing SDK...");
                
                if (string.IsNullOrEmpty(clientId) || clientId == "your-client-id-here")
                {
                    LogError("Error: Please configure a valid ClientId first");
                    UpdateStatusText("Initialization failed: ClientId not configured");
                    return;
                }

                LogMessage($"ClientId: {clientId}");
                LogMessage("Starting OneDrive SDK initialization...");

                var result = await Api.OneDrive.InitializeAsync(
                    clientId: clientId,
                    enableDetailedLogging: true,
                    autoCopyToClipboard: true,
                    autoOpenBrowser: true);

                if (result.IsSuccess)
                {
                    isInitialized = true;
                    LogMessage("SDK initialization successful!");
                    UpdateStatusText("SDK initialized");
                }
                else
                {
                    LogError($"SDK initialization failed: {result.Error}");
                    UpdateStatusText($"Initialization failed: {result.Error}");
                }

                UpdateButtonStates();
                LogMessage("========== SDK Initialization Test Complete ==========\n");
            }
            catch (Exception ex)
            {
                LogError($"Initialization exception: {ex.Message}");
                UpdateStatusText($"Initialization exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Test 2: User Authentication Process
        /// </summary>
        public async Task AuthenticateAsync()
        {
            try
            {
                LogMessage("========== Starting Test: User Authentication ==========");
                UpdateStatusText("Authenticating user...");

                if (!isInitialized)
                {
                    LogError("Error: SDK not initialized, please initialize first");
                    return;
                }

                LogMessage("Attempting quick authentication...");

                // First try cached authentication
                var quickResult = await Api.OneDrive.QuickAuthenticateAsync();
                
                if (quickResult.IsSuccess)
                {
                    isAuthenticated = true;
                    LogMessage("Quick authentication successful!");
                    UpdateStatusText("Authenticated (cached)");
                }
                else
                {
                    LogMessage($"Quick authentication failed: {quickResult.Error}");
                    LogMessage("Starting device code authentication flow...");
                    UpdateStatusText("Device code authentication required");

                    // Use device code authentication
                    var authResult = await Api.OneDrive.AuthenticateAsync(
                        onCodeReady: CSharpFunctionalExtensions.Maybe<Action<DeviceCodeResult>>.From(OnDeviceCodeReady));

                    if (authResult.IsSuccess)
                    {
                        isAuthenticated = true;
                        LogMessage("Device code authentication successful!");
                        UpdateStatusText("Authenticated (device code)");
                    }
                    else
                    {
                        LogError($"Authentication failed: {authResult.Error}");
                        UpdateStatusText($"Authentication failed: {authResult.Error}");
                    }
                }

                UpdateButtonStates();
                LogMessage("========== User Authentication Test Complete ==========\n");
            }
            catch (Exception ex)
            {
                LogError($"Authentication exception: {ex.Message}");
                UpdateStatusText($"Authentication exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Device code callback handler
        /// </summary>
        private void OnDeviceCodeReady(DeviceCodeResult deviceCodeResult)
        {
            LogMessage("Device Code Information:");
            LogMessage($"   Device Code: {deviceCodeResult.UserCode}");
            LogMessage($"   Verification URL: {deviceCodeResult.VerificationUrl}");
            LogMessage($"   Expires at: {deviceCodeResult.ExpiresOn:HH:mm:ss}");
            LogMessage("Please open the verification URL in browser and enter the device code");
            
            // On Android, users need to manually copy URL and device code
            LogMessage("Android Operation Instructions:");
            LogMessage("1. Manually copy the verification URL above");
            LogMessage("2. Open the URL in browser");
            LogMessage("3. Enter the device code for verification");
        }

        /// <summary>
        /// Test 3: Get User Information
        /// </summary>
        public async Task GetUserInfoAsync()
        {
            try
            {
                LogMessage("========== Starting Test: Get User Information ==========");
                UpdateStatusText("Getting user information...");

                if (!isAuthenticated)
                {
                    LogError("Error: User not authenticated, please authenticate first");
                    return;
                }

                var userResult = await Api.OneDrive.GetUserAsync();

                if (userResult.IsSuccess)
                {
                    var user = userResult.Value;
                    LogMessage("User information retrieved successfully:");
                    LogMessage($"   Display Name: {user.DisplayName ?? "Not set"}");
                    LogMessage($"   Email: {user.Mail ?? user.UserPrincipalName ?? "Not set"}");
                    LogMessage($"   User ID: {user.Id ?? "Not set"}");
                    LogMessage($"   Country: {user.Country ?? "Not set"}");
                    LogMessage($"   Language: {user.PreferredLanguage ?? "Not set"}");
                    LogMessage($"   Job Title: {user.JobTitle ?? "Not set"}");
                    LogMessage($"   Mobile Phone: {user.MobilePhone ?? "Not set"}");
                    LogMessage($"   Office Location: {user.OfficeLocation ?? "Not set"}");
                    
                    UpdateStatusText($"User: {user.DisplayName}");
                }
                else
                {
                    LogError($"Failed to get user information: {userResult.Error}");
                    UpdateStatusText($"Failed to get user information");
                }

                LogMessage("========== Get User Information Test Complete ==========\n");
            }
            catch (Exception ex)
            {
                LogError($"Get user information exception: {ex.Message}");
                UpdateStatusText($"Get user information exception");
            }
        }

        /// <summary>
        /// Test 4: Get File List
        /// </summary>
        public async Task GetFilesAsync()
        {
            try
            {
                LogMessage("========== Starting Test: Get File List ==========");
                UpdateStatusText("Getting file list...");

                if (!isAuthenticated)
                {
                    LogError("Error: User not authenticated, please authenticate first");
                    return;
                }

                // First get drive information
                var driveResult = await Api.OneDrive.GetDriveAsync();
                if (driveResult.IsSuccess)
                {
                    var drive = driveResult.Value;
                    LogMessage("Drive Information:");
                    LogMessage($"   Name: {drive.Name ?? "Not set"}");
                    LogMessage($"   Type: {drive.DriveType ?? "Unknown"}");
                    
                    if (drive.Quota != null)
                    {
                        LogMessage($"   Total Capacity: {FormatBytes(drive.Quota.Total ?? 0)}");
                        LogMessage($"   Used: {FormatBytes(drive.Quota.Used ?? 0)}");
                        LogMessage($"   Remaining: {FormatBytes(drive.Quota.Remaining ?? 0)}");
                    }
                }

                // Get root directory file list
                var filesResult = await Api.OneDrive.GetFilesAsync();

                if (filesResult.IsSuccess)
                {
                    var files = filesResult.Value.Value;
                    LogMessage($"File list retrieved successfully ({files.Count} items):");
                    
                    if (files.Count == 0)
                    {
                        LogMessage("   (Root directory is empty)");
                    }
                    else
                    {
                        int displayCount = Math.Min(files.Count, 20); // Display maximum 20 items
                        for (int i = 0; i < displayCount; i++)
                        {
                            var item = files[i];
                            var type = item.Folder != null ? "Folder" : "File";
                            var size = item.Size.HasValue ? FormatBytes(item.Size.Value) : "--";
                            var modified = item.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown";
                            
                            LogMessage($"   {type} {item.Name}");
                            LogMessage($"      Size: {size}, Modified: {modified}");
                        }
                        
                        if (files.Count > 20)
                        {
                            LogMessage($"   ... {files.Count - 20} more items not displayed");
                        }
                    }
                    
                    UpdateStatusText($"File list: {files.Count} items");
                }
                else
                {
                    LogError($"Failed to get file list: {filesResult.Error}");
                    UpdateStatusText("Failed to get file list");
                }

                LogMessage("========== Get File List Test Complete ==========\n");
            }
            catch (Exception ex)
            {
                LogError($"Get file list exception: {ex.Message}");
                UpdateStatusText("Get file list exception");
            }
        }

        /// <summary>
        /// User Sign Out
        /// </summary>
        public async Task SignOutAsync()
        {
            try
            {
                LogMessage("========== Starting Sign Out ==========");
                UpdateStatusText("Signing out...");

                var result = await Api.OneDrive.SignOutAsync();
                
                if (result.IsSuccess)
                {
                    isAuthenticated = false;
                    LogMessage("Sign out successful");
                    UpdateStatusText("Signed out");
                }
                else
                {
                    LogError($"Sign out failed: {result.Error}");
                    UpdateStatusText($"Sign out failed: {result.Error}");
                }

                UpdateButtonStates();
                LogMessage("========== Sign Out Complete ==========\n");
            }
            catch (Exception ex)
            {
                LogError($"Sign out exception: {ex.Message}");
                UpdateStatusText("Sign out exception");
            }
        }

        /// <summary>
        /// Update button states
        /// </summary>
        private void UpdateButtonStates()
        {
            if (initializeButton != null)
                initializeButton.interactable = !isInitialized;
            
            if (authenticateButton != null)
                authenticateButton.interactable = isInitialized && !isAuthenticated;
            
            if (getUserInfoButton != null)
                getUserInfoButton.interactable = isAuthenticated;
            
            if (getFilesButton != null)
                getFilesButton.interactable = isAuthenticated;
            
            if (signOutButton != null)
                signOutButton.interactable = isAuthenticated;
        }

        /// <summary>
        /// Update status text
        /// </summary>
        private void UpdateStatusText(string status)
        {
            if (statusText != null)
                statusText.text = $"Status: {status}";
        }

        /// <summary>
        /// Log message
        /// </summary>
        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            logBuilder.AppendLine(logEntry);
            UpdateLogDisplay();
            
            Debug.Log($"[AndroidOneDriveTest] {message}");
        }

        /// <summary>
        /// Log error message
        /// </summary>
        private void LogError(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] ERROR: {message}";
            
            logBuilder.AppendLine(logEntry);
            UpdateLogDisplay();
            
            Debug.LogError($"[AndroidOneDriveTest] {message}");
        }

        /// <summary>
        /// Update log display
        /// </summary>
        private void UpdateLogDisplay()
        {
            if (logText != null)
            {
                logText.text = logBuilder.ToString();
                
                // Auto scroll to bottom
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        /// <summary>
        /// Format bytes size
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Clear log
        /// </summary>
        [ContextMenu("Clear Log")]
        public void ClearLog()
        {
            logBuilder.Clear();
            UpdateLogDisplay();
        }

        /// <summary>
        /// Execute full test workflow
        /// </summary>
        [ContextMenu("Execute Full Test")]
        public async void RunFullTest()
        {
            LogMessage("Starting full test workflow...");
            
            await InitializeSDKAsync();
            if (!isInitialized) return;
            
            await Task.Delay(1000);
            await AuthenticateAsync();
            if (!isAuthenticated) return;
            
            await Task.Delay(1000);
            await GetUserInfoAsync();
            
            await Task.Delay(1000);
            await GetFilesAsync();
            
            LogMessage("Full test workflow completed!");
        }

        private void OnDestroy()
        {
            // Clean up resources
            Api.OneDrive.Dispose();
        }

        private void OnValidate()
        {
            // Display current status in Inspector
            isInitialized = Api.OneDrive.IsInitialized;
            isAuthenticated = Api.OneDrive.IsAuthenticated;
        }
    }
}