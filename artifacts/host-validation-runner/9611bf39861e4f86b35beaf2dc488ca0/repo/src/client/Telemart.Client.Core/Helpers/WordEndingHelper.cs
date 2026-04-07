using System;

namespace Telemart.Client.Core.Helpers
{
    public static class WordEndingHelper
    {
        private static readonly int[] Cases = { 2, 0, 1, 1, 1, 2 };

        public static string GetWordByNumber(int number, string[] possibleWords)
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            if (possibleWords == null)
            {
                throw new ArgumentNullException(nameof(possibleWords));
            }

            if (possibleWords.Length != 3)
            {
                throw new ArgumentException("Titles array should contain 3 items", nameof(possibleWords));
            }

            int index = number == 0 || (number % 100 > 4 && number % 100 < 20)
                ? 2
                : Cases[Math.Min(number % 10, 5)];

            return possibleWords[index];
        }

        public static string GetWordByNumber(int number, string possibleOneWord, string possibleTwoWord, string possibleFiveWord)
        {
            return GetWordByNumber(number, new[] { possibleOneWord, possibleTwoWord, possibleFiveWord });
        }
    }
}