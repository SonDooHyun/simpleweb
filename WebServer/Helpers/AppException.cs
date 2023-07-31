using WebServer.Models;

namespace WebServer.Helpers
{
    public sealed class AppException : Exception
    {
        public AppException(EErrorCode errorCode, string message = null) 
            : base (string.IsNullOrEmpty(message) ? $"{errorCode}" : message)
        {
            this.ErrorCode = errorCode;
        }

        public EErrorCode ErrorCode { get; }
    }
}
