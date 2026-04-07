namespace Telemart.Client.Business
{
    public interface IPasswordGenerator
    {
        /// <summary>
        /// Generates a random password.
        /// </summary>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        /// <remarks>
        /// The length of the generated password will be determined at random. It will be no shorter than the minimum default and no longer than maximum default.
        /// </remarks>
        string Generate();

        /// <summary>
        /// Generates a random password of the exact length.
        /// </summary>
        /// <param name="length">Exact password length.</param>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        string Generate(int length);

        /// <summary>
        /// Generates a random password.
        /// </summary>
        /// <param name="minLength">Minimum password length.</param>
        /// <param name="maxLength">Maximum password length.</param>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        /// <remarks>
        /// The length of the generated password will be determined at random and it will fall with the range determined by the function parameters.
        /// </remarks>
        string Generate(int minLength, int maxLength);
    }
}