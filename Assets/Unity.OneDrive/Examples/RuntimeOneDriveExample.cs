using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using UnityEngine;

namespace Unity.OneDrive.Examples
{
    /// <summary>
    /// Runtime dynamic operation example
    /// </summary>
    public class RuntimeOneDriveExample : MonoBehaviour
    {
        [Header("Runtime Operation Hotkeys")]
        public KeyCode uploadScreenshotKey = KeyCode.U;
        public KeyCode listFilesKey = KeyCode.L;
        public KeyCode getUserInfoKey = KeyCode.I;

        private void Update()
        {
            if (!Api.OneDrive.IsAuthenticated) return;

            if (Input.GetKeyDown(uploadScreenshotKey))
                _ = UploadScreenshotAsync();

            if (Input.GetKeyDown(listFilesKey))
                _ = QuickListFilesAsync();

            if (Input.GetKeyDown(getUserInfoKey))
                _ = QuickGetUserInfoAsync();
        }

        private async Task UploadScreenshotAsync()
        {
            try
            {
                Debug.Log("Uploading screenshot...");

                // Create screenshot
                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                var imageBytes = screenshot.EncodeToPNG();
                var fileName = $"Unity_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                // Upload to OneDrive
                var graph = Api.OneDrive.GraphClient.Value;
                var drive = await graph.Me.Drive.GetAsync();

                if (drive?.Id != null)
                {
                    using var stream = new System.IO.MemoryStream(imageBytes);
                    var uploadedItem = await graph.Drives[drive.Id]
                        .Items["root"]
                        .ItemWithPath(fileName)
                        .Content
                        .PutAsync(stream);

                    Debug.Log($"Screenshot upload successful: {uploadedItem.Name}");
                }

                Destroy(screenshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Screenshot upload failed: {ex.Message}");
            }
        }

        private async Task QuickListFilesAsync()
        {
            var result = await Api.OneDrive.GetFilesAsync();
            if (result.IsSuccess)
            {
                Debug.Log($"Quick file list ({result.Value.Value.Count} items)");
            }
        }

        private async Task QuickGetUserInfoAsync()
        {
            var result = await Api.OneDrive.GetUserAsync();
            if (result.IsSuccess)
            {
                Debug.Log($"Quick user info: {result.Value.DisplayName}");
            }
        }
    }
}