namespace Telemart.Client.ViewModels.Validation
{
    public class ValidationResultMessageItem
    {
        public ValidationResultMessageItem(string header, string message)
        {
            Header = header;
            Message = message;
        }

        public string Header { get; }

        public string Message { get; }
    }
}
