using System;
using System.Linq;
using System.Text;

namespace Telemart.Client.Business.Order
{
    public sealed class Fio
    {
        public Fio(string rawFio)
        {
            (LastName, FirstName, MiddleName) = ParseFio(rawFio);
        }

        public string FirstName { get; }

        public string LastName { get; }

        public string MiddleName { get; }

        public static string CreateFioString(string lastName, string firstName, string middleName)
        {
            string[] fioParts = { lastName, firstName, middleName };

            return string.Join(' ', fioParts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static (string lastName, string firstName, string middleName) ParseFio(string rawFio)
        {
            string lastName;
            string firstName;
            string middleName;

            if (!string.IsNullOrWhiteSpace(rawFio))
            {
                string[] parts = rawFio.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                lastName = parts[0];
                firstName = parts.Length > 1
                    ? parts[1]
                    : null;
                middleName = parts.Length > 2
                    ? parts[2]
                    : null;

                if (firstName == null && middleName == null)
                {
                    firstName = lastName;
                    lastName = null;
                }
            }
            else
            {
                lastName = null;
                firstName = string.Empty;
                middleName = null;
            }

            return (lastName, firstName, middleName);
        }
    }
}