using System;

namespace Telemart.Client.Data.Authentication
{
    public sealed class AuthRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthRequest"/> class.
        /// </summary>
        /// <param name="login">The login.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="ArgumentNullException"><paramref name="login" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="password" /> is <see langword="null" />.</exception>
        public AuthRequest(string login, string password)
        {
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            Login = login;
            Password = password;
        }

        public string Login { get; }

        public string Password { get; }
    }
}