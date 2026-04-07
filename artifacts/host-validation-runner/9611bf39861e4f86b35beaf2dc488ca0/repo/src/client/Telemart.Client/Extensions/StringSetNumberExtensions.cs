using System.Collections.Generic;

namespace Telemart.Client.Extensions
{
    public static class StringSetNumberExtensions
    {
        public static string ChangeToCommaValue(this string numbers)
        {
            string newValue = numbers?.Trim();

            if (string.IsNullOrEmpty(numbers) || int.TryParse(numbers, out int _))
            {
                return newValue;
            }

            char[] chars = numbers.ToCharArray();

            List<char> charList = new List<char>();

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                {
                    charList.Add(chars[i]);
                }
                else if (charList.Count > 0 && charList[charList.Count - 1] != ',')
                {
                    charList.Add(',');
                }
            }

            string result = string.Join(string.Empty, charList);

            return result;
        }
    }
}