namespace Telemart.Client.Oktell
{
    public class OktellResult
    {
        public OktellResult(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        public OktellResult()
            : this(string.Empty, false)
        {
        }

        public string Message { get; }

        public bool IsError { get; }

        public bool IsOk => !IsError;
    }
}
