namespace TXT.WEAVR.Communication
{
    public static class MIME
    {
        // TEXT BASED
        public const string DEFAULT_TEXT = "text/plain";
        public const string CSS = "text/css";
        public const string CSV = "text/csv";
        public const string HTML = "text/html";
        public const string JAVASCRIPT = "text/javascript";

        // APPLICATION BASED
        public const string DEFAULT_BYTES = "application/octet-stream";
        public const string JSON = "application/json";
        public const string GZIP = "application/gzip";
        public const string ZIP = "application/zip";
        public const string PDF = "application/pdf";

        // OTHER
        public const string GIF = "image/gif";
        public const string JPEG = "image/jpeg";
        public const string PNG = "image/png";
        public const string MP3 = "audio/mpeg";
        public const string MPEG = "video/mpeg";

    }

    public static class RequestHeader
    {
        public const string AUTHORIZATION = "Authorization";
        public const string CONTENT_TYPE = "Content-Type";
    }

    public enum HttpStatusCode : int
    {
        // Informational
        Continue = 100,
        SwitchingProtocols = 101,

        // Success
        OK = 200,
        Created = 201,
        Accepted = 202,
        NoContent = 204,

        // Redirection
        NotModified = 304,

        // Client Error
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        RequestTimeout = 408,
        Conflict = 409,

        // Server Error
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
    } 
}