namespace Telemart.Client.Data.Authentication
{
    public sealed class AuthResponse
    {
        public AuthResponse(int userId)
        {
            UserId = userId;
            Success = true;
            ErrorMessage = string.Empty;
        }

        public AuthResponse(string errorMessage)
        {
            Success = false;
            ErrorMessage = errorMessage;
        }

        public int UserId { get; }

        public bool Success { get; }

        public string ErrorMessage { get; }
    }
}