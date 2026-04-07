using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Telemart.Client.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool CompareWildcard(this string source, string toFind)
        {
            bool isMatch;

            try
            {
                string toFindPattern = new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\+|\?|\\")
                    .Replace(toFind, ch => $@"\{ch}")
                    .Replace("*", ".*")
                    .Replace(@"\?", @".?");

                toFindPattern = string.Concat(@"\A", toFindPattern, @"\Z");

                isMatch = new Regex(toFindPattern, RegexOptions.IgnoreCase).IsMatch(source);
            }
            catch (RegexMatchTimeoutException)
            {
                isMatch = false;
            }

            return isMatch;
        }

        public static bool ContainsWildcard(this string source, string toCheck)
        {
            return CompareWildcard(source, string.Concat("*", toCheck, "*"));
        }

        public static string JoinSmart(this string source, string toConcat)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(toConcat))
            {
                result = string.Equals(source, toConcat, StringComparison.Ordinal)
                    ? source
                    : $"{source} ({toConcat})";
            }
            else if (!string.IsNullOrWhiteSpace(source))
            {
                result = source;
            }
            else if (!string.IsNullOrWhiteSpace(toConcat))
            {
                result = toConcat;
            }

            return result;
        }

        public static bool TryParseInt32(string source, out int value)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                value = 0;
                return false;
            }

            string s = source.RemoveExceptSymbols("0123456789");
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParseDouble(this string source, out double value)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                value = 0;
                return false;
            }

            string s = source.Replace(",", ".").RemoveExceptSymbols("0123456789.");

            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static string RemoveExceptSymbols(this string source, string pattern)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in source)
            {
                if (pattern.IndexOf(c) > -1)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        public static string TrimEnd(this string source, string suffixToRemove, StringComparison comparisonType)
        {
            return source != null && suffixToRemove != null && source.EndsWith(suffixToRemove, comparisonType)
                ? source.Substring(0, source.Length - suffixToRemove.Length)
                : source;
        }

        public static string SetSimbolThroughNumberCharacters(this string source, int throughTheNumberCharacters, string simbol)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            string result = source.Replace(simbol, "");

            if (result?.Length > throughTheNumberCharacters)
            {
                int maxCount = result.Length / throughTheNumberCharacters;

                int i = 0;

                do
                {
                    if (i != 0)
                    {
                        result = result.Insert(i * throughTheNumberCharacters, simbol);
                    }
                } while (i++ < maxCount);
            }

            return result;
        }

        public static string CutString(this string source, int maxLenghtComment, bool setEllipsis = false)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            string result = source.Length > maxLenghtComment ? source.Substring(0, maxLenghtComment) : source;

            return setEllipsis && source.Length > maxLenghtComment ? $"{result}..." : result;
        }

        public static string GetLastPartSubString(this string source, int count)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            string text = source.Trim();

            int startIndex = text.Length - count;

            if (startIndex < 0)
            {
                return string.Empty;
            }

            return text.Substring(startIndex);
        }

        public static string GetLocalPhoneNumber(this string source)
        {
            string resultSource = source?.Replace(" ", string.Empty);

            if (string.IsNullOrEmpty(resultSource))
            {
                return string.Empty;
            }

            int startIndex = resultSource.StartsWith("+380") ? 3
                : resultSource.StartsWith("380") ? 2
                : 0;

            if (startIndex > 0)
            {
                resultSource = resultSource.Substring(startIndex);
            }

            if (resultSource.Length > 10)
            {
                resultSource = resultSource.Substring(0, 10);
            }

            return resultSource;
        }

        public static string ToUpperFirstLetter(this string source)
        {
            return string.IsNullOrEmpty(source) ? source : $"{source.Substring(0, 1).ToUpper()}{source.Substring(1)}";
        }

        public static string GetStringWithPrefix(this string source, string prefix)
        {
            return string.IsNullOrWhiteSpace(prefix) || source.StartsWith(prefix.Trim()) ? source : $"{prefix.Trim()} {source}";
        }
    }
}