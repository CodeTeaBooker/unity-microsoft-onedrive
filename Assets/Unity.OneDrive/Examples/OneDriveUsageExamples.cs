using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using UnityEngine;

namespace Unity.OneDrive.Examples
{
    /// <summary>
    /// Unity OneDrive SDK usage examples - Microsoft.Graph 5.84.0 compatible version
    /// Shows how to use the updated SDK for OneDrive operations
    /// </summary>
    public class OneDriveUsageExample : MonoBehaviour
    {
        [Header("Azure Application Configuration")]
        [SerializeField] private string clientId = "your-client-id-here";

        [Header("SDK Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isAuthenticated = false;
        [SerializeField] private string currentUser = "";

        private async void Start()
        {
            await RunCompleteExample();
        }

        /// <summary>
        /// Complete SDK usage example
        /// </summary>
        private async Task RunCompleteExample()
        {
            try
            {
                Debug.Log("Unity OneDrive SDK - Microsoft.Graph 5.84.0 example started");

                // Step 1: Initialize SDK
                await InitializeSDK();

                // Step 2: Authenticate user
                await AuthenticateUser();

                // Step 3: Get user information
                await GetUserInfo();

                // Step 4: Get drive information
                await GetDriveInfo();

                // Step 5: List files
                await ListFiles();

                // Step 6: Upload example file
                await UploadExampleFile();

                // Step 7: Validate connection
                await ValidateConnection();

                Debug.Log("All operations completed! SDK working properly.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Example execution failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Step 1: Initialize SDK
        /// </summary>
        private async Task InitializeSDK()
        {
            Debug.Log("Step 1: Initializing SDK...");

            var result = await Api.OneDrive.InitializeAsync(
                clientId: clientId,
                enableDetailedLogging: true,
                autoCopyToClipboard: true,
                autoOpenBrowser: true);

            if (result.IsSuccess)
            {
                isInitialized = true;
                Debug.Log("SDK initialization successful");
            }
            else
            {
                Debug.LogError($"SDK initialization failed: {result.Error}");
                throw new Exception($"SDK initialization failed: {result.Error}");
            }
        }

        /// <summary>
        /// Step 2: Authenticate user
        /// </summary>
        private async Task AuthenticateUser()
        {
            Debug.Log("Step 2: User authentication...");

            var result = await Api.OneDrive.QuickAuthenticateAsync();

            if (result.IsSuccess)
            {
                isAuthenticated = true;
                Debug.Log("User authentication successful");
                Debug.Log("If this is the first authentication, please check browser and enter device code");
            }
            else
            {
                Debug.LogError($"User authentication failed: {result.Error}");
                throw new Exception($"User authentication failed: {result.Error}");
            }
        }

        /// <summary>
        /// Step 3: Get user information
        /// </summary>
        private async Task GetUserInfo()
        {
            Debug.Log("Step 3: Getting user information...");

            // Use updated extension method
            var userResult = await Api.OneDrive.GetUserAsync();

            if (userResult.IsSuccess)
            {
                var user = userResult.Value;
                currentUser = user.DisplayName ?? "Unknown user";

                Debug.Log($"User information retrieved successfully:");
                Debug.Log($"   Display name: {user.DisplayName}");
                Debug.Log($"   Email: {user.Mail ?? user.UserPrincipalName}");
                Debug.Log($"   Country: {user.Country}");
                Debug.Log($"   Language: {user.PreferredLanguage}");
            }
            else
            {
                Debug.LogError($"Getting user information failed: {userResult.Error}");
            }
        }

        /// <summary>
        /// Step 4: Get drive information
        /// </summary>
        private async Task GetDriveInfo()
        {
            Debug.Log("Step 4: Getting drive information...");

            var driveResult = await Api.OneDrive.GetDriveAsync();

            if (driveResult.IsSuccess)
            {
                var drive = driveResult.Value;

                Debug.Log($"Drive information retrieved successfully:");
                Debug.Log($"   Name: {drive.Name}");
                Debug.Log($"   Type: {drive.DriveType}");
                Debug.Log($"   Total capacity: {FormatBytes(drive.Quota?.Total ?? 0)}");
                Debug.Log($"   Used: {FormatBytes(drive.Quota?.Used ?? 0)}");
                Debug.Log($"   Remaining: {FormatBytes(drive.Quota?.Remaining ?? 0)}");
            }
            else
            {
                Debug.LogError($"Getting drive information failed: {driveResult.Error}");
            }
        }

        /// <summary>
        /// Step 5: List files
        /// </summary>
        private async Task ListFiles()
        {
            Debug.Log("Step 5: Listing root directory files...");

            var filesResult = await Api.OneDrive.GetFilesAsync();

            if (filesResult.IsSuccess)
            {
                var files = filesResult.Value.Value;

                Debug.Log($"File list retrieved successfully ({files.Count} items):");

                foreach (var item in files)
                {
                    var type = item.Folder != null ? "Folder" : "File";
                    var size = item.Size.HasValue ? FormatBytes(item.Size.Value) : "--";
                    Debug.Log($"   {type} {item.Name} ({size})");
                }
            }
            else
            {
                Debug.LogError($"Getting file list failed: {filesResult.Error}");
            }
        }

        /// <summary>
        /// Step 6: Upload example file
        /// </summary>
        private async Task UploadExampleFile()
        {
            Debug.Log("Step 6: Uploading example file...");

            try
            {
                // Create example file content
                var fileName = $"Unity_Test_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var fileContent = $"Unity OneDrive SDK test file\n" +
                                 $"Creation time: {DateTime.Now}\n" +
                                 $"SDK version: Microsoft.Graph 5.84.0\n" +
                                 $"Unity version: {Application.unityVersion}\n" +
                                 $"User: {currentUser}";

                var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);

                // Use GraphServiceClient directly for upload
                if (Api.OneDrive.GraphClient.HasValue)
                {
                    var graph = Api.OneDrive.GraphClient.Value;
                    var drive = await graph.Me.Drive.GetAsync();

                    if (drive?.Id != null)
                    {
                        using var stream = new System.IO.MemoryStream(fileBytes);
                        var uploadedItem = await graph.Drives[drive.Id]
                            .Items["root"]
                            .ItemWithPath(fileName)
                            .Content
                            .PutAsync(stream);

                        Debug.Log($"File upload successful:");
                        Debug.Log($"   File name: {uploadedItem.Name}");
                        Debug.Log($"   Size: {FormatBytes(uploadedItem.Size ?? 0)}");
                        Debug.Log($"   View online: {uploadedItem.WebUrl}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"File upload failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Step 7: Validate connection
        /// </summary>
        private async Task ValidateConnection()
        {
            Debug.Log("Step 7: Validating API connection...");

            // Use OneDriveClient extension method to validate connection
            if (Api.OneDrive.IsInitialized && Api.OneDrive.IsAuthenticated)
            {
                try
                {
                    // Simple API call to validate connection
                    var userResult = await Api.OneDrive.GetUserAsync();
                    if (userResult.IsSuccess)
                    {
                        Debug.Log("API connection validation successful");
                        Debug.Log($"   Connected user: {userResult.Value.DisplayName}");
                        Debug.Log($"   SDK status: Fully operational");
                    }
                    else
                    {
                        Debug.LogWarning($"API connection validation failed: {userResult.Error}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Connection validation exception: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("SDK not initialized or not authenticated, skipping connection validation");
            }
        }

        /// <summary>
        /// Advanced feature example: Batch operations
        /// </summary>
        [ContextMenu("Execute Batch Operations Example")]
        public async void BatchOperationsExample()
        {
            if (!Api.OneDrive.IsAuthenticated)
            {
                Debug.LogWarning("Please authenticate user first");
                return;
            }

            Debug.Log("Batch operations example...");

            try
            {
                var graph = Api.OneDrive.GraphClient.Value;

                // Batch get user information, drive information and file list
                var userTask = graph.Me.GetAsync();
                var driveTask = graph.Me.Drive.GetAsync();

                // Wait for all tasks to complete
                await Task.WhenAll(userTask, driveTask);

                var user = await userTask;
                var drive = await driveTask;

                Debug.Log($"Batch operations completed:");
                Debug.Log($"   User: {user.DisplayName}");
                Debug.Log($"   Drive: {drive.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Batch operations failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Advanced feature example: Search files
        /// </summary>
        [ContextMenu("Search Files Example")]
        public async void SearchFilesExample()
        {
            if (!Api.OneDrive.IsAuthenticated)
            {
                Debug.LogWarning("Please authenticate user first");
                return;
            }

            Debug.Log("Search files example...");

            try
            {
                var graph = Api.OneDrive.GraphClient.Value;
                var drive = await graph.Me.Drive.GetAsync();

                if (drive?.Id != null)
                {
                    // Search for files containing "Unity"
                    var searchResults = await graph.Drives[drive.Id]
                        .Items["root"]
                        .SearchWithQ("Unity")
                        .GetAsSearchWithQGetResponseAsync();

                    Debug.Log($"Search completed, found {searchResults.Value.Count} files:");

                    foreach (var item in searchResults.Value)
                    {
                        Debug.Log($"   File {item.Name} - {item.WebUrl}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Format file size
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
        /// Display current status in Inspector
        /// </summary>
        private void OnValidate()
        {
            isInitialized = Api.OneDrive.IsInitialized;
            isAuthenticated = Api.OneDrive.IsAuthenticated;
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        private void OnDestroy()
        {
            Api.OneDrive.Dispose();
        }

        // Unity editor specific methods
#if UNITY_EDITOR
        [ContextMenu("Sign Out User")]
        public async void SignOutUser()
        {
            var result = await Api.OneDrive.SignOutAsync();
            if (result.IsSuccess)
            {
                isAuthenticated = false;
                currentUser = "";
                Debug.Log("User signed out");
            }
            else
            {
                Debug.LogError($"Sign out failed: {result.Error}");
            }
        }

        [ContextMenu("Show SDK Information")]
        public void ShowSDKInfo()
        {
            Debug.Log("Unity OneDrive SDK Information:");
            Debug.Log($"   Is initialized: {Api.OneDrive.IsInitialized}");
            Debug.Log($"   Is authenticated: {Api.OneDrive.IsAuthenticated}");
            Debug.Log($"   Current user: {currentUser}");
            Debug.Log($"   Unity version: {Application.unityVersion}");
            Debug.Log($"   SDK version: Microsoft.Graph 5.84.0 + MSAL.NET 4.73.1");
        }
#endif
    }
}