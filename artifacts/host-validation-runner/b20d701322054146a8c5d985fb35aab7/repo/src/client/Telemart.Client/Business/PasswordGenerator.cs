using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Telemart.Client.Business
{
    /// <summary>
    /// This class can generate random passwords, which do not include ambiguous
    /// characters, such as I, l, and 1. The generated password will be made of
    /// 7-bit ASCII symbols. Every four characters will include one lower case
    /// character, one upper case character, one number, and one special symbol
    /// (such as '%') in a random order. The password will always start with an
    /// alpha-numeric character; it will not start with a special symbol (we do
    /// this because some back-end systems do not like certain special
    /// characters in the first position).
    /// </summary>
    public sealed class PasswordGenerator : IPasswordGenerator
    {
        public const int DefaultMinPasswordLength = 8;
        public const int DefaultMaxPasswordLength = 10;

        private static string passwordCharsLcase = "abcdefgijkmnopqrstwxyz";
        private static string passwordCharsUcase = "ABCDEFGHJKLMNPQRSTWXYZ";
        private static string passwordCharsNumeric = "23456789";
        private static string passwordCharsSpecial = "*$-+?_&=!%{}/";

        /// <summary>
        /// Generates a random password.
        /// </summary>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        /// <remarks>
        /// The length of the generated password will be determined at random. It will be no shorter than the minimum default and no longer than maximum default.
        /// </remarks>
        public string Generate()
        {
            return Generate(DefaultMinPasswordLength, DefaultMaxPasswordLength);
        }

        /// <summary>
        /// Generates a random password of the exact length.
        /// </summary>
        /// <param name="length">Exact password length.</param>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        public string Generate(int length)
        {
            return Generate(length, length);
        }

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
        public string Generate(int minLength, int maxLength)
        {
            if (minLength <= 0 || maxLength <= 0 || minLength > maxLength)
            {
                throw new InvalidOperationException();
            }

            // Create a local array containing supported password characters grouped by types.
            char[][] charGroups = GetSupportedPasswordCharacters(false).ToArray();

            // Use this array to track the number of unused characters in each character group.
            int[] charsLeftInGroup = new int[charGroups.Length];

            // Initially, all characters in each group are not used.
            for (int i = 0; i < charsLeftInGroup.Length; i++)
            {
                charsLeftInGroup[i] = charGroups[i].Length;
            }

            // Use this array to track (iterate through) unused character groups.
            int[] leftGroupsOrder = new int[charGroups.Length];

            // Initially, all character groups are not used.
            for (int i = 0; i < leftGroupsOrder.Length; i++)
            {
                leftGroupsOrder[i] = i;
            }

            Random random = Random.Shared;

            // Allocate appropriate memory for the password.
            char[] password = minLength < maxLength
                ? new char[random.Next(minLength, maxLength + 1)]
                : new char[minLength];

            // Index of the last non-processed group.
            int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            // Generate password characters one at a time.
            for (int i = 0; i < password.Length; i++)
            {
                // If only one character group remained unprocessed, process it;
                // otherwise, pick a random character group from the unprocessed
                // group list. To allow a special character to appear in the
                // first position, increment the second parameter of the Next
                // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
                int nextLeftGroupsOrderIdx = lastLeftGroupsOrderIdx == 0
                    ? 0
                    : random.Next(0, lastLeftGroupsOrderIdx);

                // Get the actual index of the character group, from which we will pick the next character.
                int nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

                // Get the index of the last unprocessed characters in this group.
                int lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

                // If only one unprocessed character is left, pick it; otherwise, get a random character from the unused character list.
                int nextCharIdx = lastCharIdx == 0
                    ? 0
                    : random.Next(0, lastCharIdx + 1);

                // Add this character to the password.
                password[i] = charGroups[nextGroupIdx][nextCharIdx];

                // If we processed the last character in this group, start over.
                if (lastCharIdx == 0)
                {
                    charsLeftInGroup[nextGroupIdx] = charGroups[nextGroupIdx].Length;
                }
                else
                {
                    // There are more unprocessed characters left.
                    // Swap processed character with the last unprocessed character
                    // so that we don't pick it until we process all characters in
                    // this group.
                    if (lastCharIdx != nextCharIdx)
                    {
                        (charGroups[nextGroupIdx][lastCharIdx], charGroups[nextGroupIdx][nextCharIdx]) = (charGroups[nextGroupIdx][nextCharIdx], charGroups[nextGroupIdx][lastCharIdx]);
                    }

                    // Decrement the number of unprocessed characters in this group.
                    charsLeftInGroup[nextGroupIdx]--;
                }

                // If we processed the last group, start all over.
                if (lastLeftGroupsOrderIdx == 0)
                {
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                }
                else
                {
                    // There are more unprocessed groups left.
                    // Swap processed group with the last unprocessed group so that we don't pick it until we process all groups.
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        (leftGroupsOrder[lastLeftGroupsOrderIdx], leftGroupsOrder[nextLeftGroupsOrderIdx]) = (leftGroupsOrder[nextLeftGroupsOrderIdx], leftGroupsOrder[lastLeftGroupsOrderIdx]);
                    }

                    // Decrement the number of unprocessed groups.
                    lastLeftGroupsOrderIdx--;
                }
            }

            // Convert password characters into a string and return the result.
            return new string(password);
        }

        private IEnumerable<char[]> GetSupportedPasswordCharacters(bool useSpecial)
        {
            yield return passwordCharsLcase.ToCharArray();
            yield return passwordCharsUcase.ToCharArray();
            yield return passwordCharsNumeric.ToCharArray();

            if (useSpecial)
            {
                yield return passwordCharsSpecial.ToCharArray();
            }
        }
    }
}