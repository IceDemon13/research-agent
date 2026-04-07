namespace Telemart.Client.Core.Extensions
{
    public static class CharExtensions
    {
        public static bool IsBasicLatin(this char charToCheck)
        {
            return charToCheck >= 'A' && charToCheck <= 'z';
        }

        public static bool IsCyrillic(this char charToCheck)
        {
            return charToCheck >= 'Ѐ' && charToCheck <= 'ӿ';
        }
    }
}