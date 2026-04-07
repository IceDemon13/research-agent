namespace Telemart.Client.TransferObjects.WorkAccount
{
    public sealed class TelewikiCreateAccountDto
    {
        public TelewikiCreateAccountDto(int id, string password)
        {
            Id = id;
            Password = password;
        }

        public int Id { get; }

        public string Password { get; }
    }
}