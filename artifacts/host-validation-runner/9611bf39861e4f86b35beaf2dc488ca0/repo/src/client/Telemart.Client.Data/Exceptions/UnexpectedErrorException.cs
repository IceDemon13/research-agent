namespace Telemart.Client.Core.Exceptions
{
    public sealed class UnexpectedErrorException : GenericException<ExceptionArgs>
    {
        public UnexpectedErrorException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}