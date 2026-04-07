namespace Telemart.Client.Helpers
{
    public static class AddressHelper
    {
        public static string GetFullAddress(string house, string flatNumber, string comment, string placeName)
        {
            string fullAddressString;

            if (placeName != null)
            {
                string flatString = !string.IsNullOrWhiteSpace(flatNumber)
                    ? $", кв. {flatNumber}"
                    : string.Empty;

                string commentString = !string.IsNullOrEmpty(comment)
                    ? $" ({comment})"
                    : string.Empty;

                fullAddressString = $"{placeName}, {house}{flatString}{commentString}";
            }
            else
            {
                fullAddressString = string.Empty;
            }

            return fullAddressString;
        }
    }
}