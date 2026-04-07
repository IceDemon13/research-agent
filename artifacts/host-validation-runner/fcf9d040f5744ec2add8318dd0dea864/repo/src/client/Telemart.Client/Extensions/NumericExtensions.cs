namespace Telemart.Client.Extensions
{
    public static class NumericExtensions
    {
        public static int ToInches(this int num)
        {
            return (int)(num / 2.1);
        }
    }
}
