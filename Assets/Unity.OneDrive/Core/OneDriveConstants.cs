namespace Unity.OneDrive.Core
{
    /// <summary>
    /// Unity OneDrive SDK 常量定义
    /// 避免硬编码，提高代码可维护性
    /// </summary>
    public static class OneDriveConstants
    {
        #region 认证相关常量
        /// <summary>
        /// Microsoft 认证授权URL
        /// </summary>
        public const string MICROSOFT_AUTHORITY_URL = "https://login.microsoftonline.com/common";

        /// <summary>
        /// 本地客户端重定向URI
        /// </summary>
        public const string NATIVE_CLIENT_REDIRECT_URI = "https://login.microsoftonline.com/common/oauth2/nativeclient";

        /// <summary>
        /// HTTP Authorization 头名称
        /// </summary>
        public const string AUTHORIZATION_HEADER = "Authorization";

        /// <summary>
        /// Bearer token 前缀
        /// </summary>
        public const string BEARER_TOKEN_PREFIX = "Bearer ";
        #endregion

        #region OAuth 作用域
        /// <summary>
        /// OneDrive 文件读写权限
        /// </summary>
        public const string SCOPE_FILES_READWRITE_ALL = "Files.ReadWrite.All";

        /// <summary>
        /// 用户基本信息读取权限
        /// </summary>
        public const string SCOPE_USER_READ = "User.Read";

        /// <summary>
        /// 默认作用域数组
        /// </summary>
        public static readonly string[] DEFAULT_SCOPES = { SCOPE_FILES_READWRITE_ALL, SCOPE_USER_READ };
        #endregion

        #region 存储键名
        /// <summary>
        /// Unity PlayerPrefs 中存储 token cache 的键名
        /// </summary>
        public const string TOKEN_CACHE_KEY = "Unity.OneDrive.TokenCache";
        #endregion

        #region 超时和延迟设置
        /// <summary>
        /// 主线程操作超时时间（秒）
        /// </summary>
        public const int MAIN_THREAD_OPERATION_TIMEOUT_SECONDS = 5;

        /// <summary>
        /// 操作完成后的标准延迟时间（毫秒）
        /// </summary>
        public const int STANDARD_OPERATION_DELAY_MS = 50;

        /// <summary>
        /// Token 刷新提前时间（分钟）
        /// </summary>
        public const int TOKEN_REFRESH_ADVANCE_MINUTES = 5;

        /// <summary>
        /// 默认 Token 有效期（小时）
        /// </summary>
        public const int DEFAULT_TOKEN_EXPIRY_HOURS = 1;
        #endregion

        #region 日志相关常量
        /// <summary>
        /// 日志前缀
        /// </summary>
        public const string LOG_PREFIX = "[OneDrive]";

        /// <summary>
        /// 详细日志时间格式
        /// </summary>
        public const string DETAILED_LOG_TIME_FORMAT = "HH:mm:ss.fff";

        /// <summary>
        /// 日志级别
        /// </summary>
        public static class LogLevels
        {
            public const string INFO = "INFO";
            public const string WARN = "WARN";
            public const string ERROR = "ERROR";
        }
        #endregion

        #region SDK 信息
        /// <summary>
        /// SDK 版本信息
        /// </summary>
        public const string SDK_VERSION_INFO = "Microsoft.Graph 5.84.0 + Kiota Integration + MSAL.NET 4.73.1";

        /// <summary>
        /// SDK 名称
        /// </summary>
        public const string SDK_NAME = "Unity OneDrive SDK";
        #endregion

        #region 文件操作相关
        /// <summary>
        /// OneDrive 根目录标识符
        /// </summary>
        public const string ROOT_FOLDER_ID = "root";

        /// <summary>
        /// 文件名时间戳格式
        /// </summary>
        public const string FILE_TIMESTAMP_FORMAT = "yyyyMMdd_HHmmss";

        /// <summary>
        /// 支持的图像格式扩展名
        /// </summary>
        public static class ImageFormats
        {
            public const string PNG = ".png";
            public const string JPG = ".jpg";
            public const string JPEG = ".jpeg";
        }

        /// <summary>
        /// 文件名模板
        /// </summary>
        public static class FileNameTemplates
        {
            public const string UNITY_TEST_FILE = "Unity_Test_{0}.txt";
            public const string UNITY_SCREENSHOT = "Unity_Screenshot_{0}.png";
        }
        #endregion

        #region 日志消息模板
        /// <summary>
        /// 常用日志消息模板
        /// </summary>
        public static class LogMessages
        {
            // 初始化相关
            public const string SDK_INITIALIZING = "Initializing SDK...";
            public const string SDK_INITIALIZATION_SUCCESS = "SDK initialization successful";
            public const string SDK_INITIALIZATION_FAILED = "SDK initialization failed: {0}";

            // 认证相关
            public const string AUTHENTICATION_STARTING = "Starting Device Code Flow...";
            public const string AUTHENTICATION_SUCCESS = "Authentication successful: {0}";
            public const string AUTHENTICATION_FAILED = "Authentication failed: {0}";
            public const string CACHED_AUTH_SUCCESS = "Cached authentication successful: {0}";
            public const string SIGN_OUT_SUCCESS = "Sign out successful";

            // 浏览器操作
            public const string BROWSER_OPENED_SUCCESS = "Browser opened successfully";
            public const string BROWSER_OPENED_SUCCESS_SYNC = "Browser opened successfully (via SynchronizationContext)";
            public const string BROWSER_OPEN_FAILED = "Browser open failed: {0}";
            public const string BROWSER_TIMEOUT = "Timeout opening browser via SynchronizationContext";
            public const string BROWSER_MANUAL_OPEN = "Please open this URL manually: {0}";
            public const string BROWSER_BACKGROUND_THREAD = "Cannot open browser from background thread";

            // 剪贴板操作
            public const string CLIPBOARD_COPY_SUCCESS = "Text copied to clipboard successfully";
            public const string CLIPBOARD_COPY_SUCCESS_SYNC = "Text copied to clipboard successfully (via SynchronizationContext)";
            public const string CLIPBOARD_ACCESS_FAILED = "Clipboard access failed: {0}";
            public const string CLIPBOARD_TIMEOUT = "Timeout copying to clipboard via SynchronizationContext";
            public const string CLIPBOARD_BACKGROUND_THREAD = "Cannot access clipboard from background thread - please copy manually";
            public const string CLIPBOARD_DEVICE_CODE = "Device code: {0}";

            // Token 操作
            public const string TOKEN_SAVED_SUCCESS = "Token saved successfully";
            public const string TOKEN_SAVE_FAILED = "Failed to save token to PlayerPrefs";
            public const string TOKEN_LOAD_TIMEOUT = "Timeout reading token from PlayerPrefs";

            // Graph 客户端
            public const string GRAPH_CLIENT_CREATING = "Creating GraphServiceClient (Kiota integration)...";
            public const string GRAPH_CLIENT_CREATED = "GraphServiceClient created successfully (Kiota integration)";
            public const string GRAPH_CLIENT_CREATION_FAILED = "GraphServiceClient creation failed";
            public const string CONNECTION_VALIDATION_SUCCESS = "Connection validation successful: {0}";
            public const string CONNECTION_VALIDATION_FAILED = "Connection validation failed";

            // 自动化
            public const string AUTOMATION_COMPLETED = "Automation completed! Code: {0}";
            public const string AUTOMATION_FAILED = "Automation failed";

            // 一般警告和错误
            public const string NO_SYNC_CONTEXT = "No SynchronizationContext found, automation features may not work properly";
            public const string NO_SYNC_CONTEXT_AVAILABLE = "No SynchronizationContext available, token not saved";
        }
        #endregion

        #region 错误消息
        /// <summary>
        /// 错误消息常量
        /// </summary>
        public static class ErrorMessages
        {
            public const string NOT_INITIALIZED = "SDK not initialized";
            public const string NOT_AUTHENTICATED = "Not authenticated";
            public const string CLIENT_ID_EMPTY = "ClientId cannot be empty";
            public const string URL_EMPTY = "URL cannot be empty";
            public const string TEXT_EMPTY = "Text cannot be empty";
            public const string INVALID_URL_FORMAT = "Invalid URL format";
            public const string BROWSER_NOT_AVAILABLE = "Browser not available";
            public const string CLIPBOARD_NOT_AVAILABLE = "Clipboard not available";
            public const string NO_CACHED_ACCOUNT = "No cached account";
            public const string TOKEN_EXPIRED = "Access token has expired, re-authentication required";
            public const string UNABLE_GET_DRIVE = "Unable to get Drive";
            public const string UNABLE_GET_TOKEN = "Unable to get valid token: {0}";
            public const string UNKNOWN_ERROR = "Unknown error occurred";
        }
        #endregion

        #region 示例和测试相关
        /// <summary>
        /// 示例相关常量
        /// </summary>
        public static class Examples
        {
            public const string DEFAULT_CLIENT_ID_PLACEHOLDER = "your-client-id-here";

            // 文件内容模板
            public const string TEST_FILE_CONTENT_TEMPLATE =
                "Unity OneDrive SDK test file\n" +
                "Creation time: {0}\n" +
                "SDK version: {1}\n" +
                "Unity version: {2}\n" +
                "User: {3}";

            // 按键说明
            public static class HotkeyDescriptions
            {
                public const string UPLOAD_SCREENSHOT = "Upload Screenshot";
                public const string LIST_FILES = "List Files";
                public const string GET_USER_INFO = "Get User Info";
            }
        }
        #endregion

        #region 文件大小格式化
        /// <summary>
        /// 文件大小单位
        /// </summary>
        public static readonly string[] FILE_SIZE_UNITS = { "B", "KB", "MB", "GB", "TB" };

        /// <summary>
        /// 文件大小格式化精度
        /// </summary>
        public const string FILE_SIZE_FORMAT = "0.##";

        /// <summary>
        /// 字节到KB的转换因子
        /// </summary>
        public const int BYTES_TO_KB_FACTOR = 1024;
        #endregion

        #region Unity 相关
        /// <summary>
        /// Unity 主线程 ID（通常为1）
        /// </summary>
        public const int UNITY_MAIN_THREAD_ID = 1;
        #endregion
    }
}