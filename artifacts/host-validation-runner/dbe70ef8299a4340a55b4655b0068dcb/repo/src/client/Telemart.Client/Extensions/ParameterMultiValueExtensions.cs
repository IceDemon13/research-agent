namespace Telemart.Client.Extensions
{
    public static class ParameterMultiValueExtensions
    {
        public static T GetValueOrDafault<T>(this object[] values, int index, T defaultValue = default(T))
        {
            if (index < values?.Length && index >= 0 && values[index] is T value)
            {
                return value;
            }

            return defaultValue;
        }
    }
}