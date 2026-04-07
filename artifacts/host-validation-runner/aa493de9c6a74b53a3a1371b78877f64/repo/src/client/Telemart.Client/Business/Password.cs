using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace Telemart.Client.Business
{
    public sealed class Password
    {
        private const int MinLength = 8;
        private const string Regex = @"^(?=.*[A-Za-zА-Яа-я])(?=.*\d).+$";

        public Password(string password)
        {
            Value = password;

            Errors = Validate(Value).ToList();
            IsValid = !Errors.Any();
        }

        public IReadOnlyCollection<ValidationResult> Errors { get; }

        public bool IsValid { get; }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }

        private static IEnumerable<ValidationResult> Validate(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength || !new Regex(Regex).IsMatch(password))
            {
                yield return new ValidationResult($"Пароль должен быть не менее {MinLength} симв. и содержать буквы и цифры");
            }
        }
    }
}