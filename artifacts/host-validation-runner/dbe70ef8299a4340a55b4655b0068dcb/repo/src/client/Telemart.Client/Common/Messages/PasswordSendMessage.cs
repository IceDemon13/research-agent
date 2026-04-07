namespace Telemart.Client.Common.Messages
{
    public sealed class PasswordSendMessage
    {
        public PasswordSendMessage(string password)
        {
            Password = password;
        }

        public string Password { get; }
    }
}