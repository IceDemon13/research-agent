using System;

namespace Telemart.Client.Helpers
{
    public static class ProductHelper
    {
        public const int ProductEditingOvertimeDays = 30;

        public static bool IsProductEditingOvertimed(DateTime createdOn)
        {
            return (DateTime.Now - createdOn).TotalDays > ProductEditingOvertimeDays;
        }

        public static decimal? GetExtraCharge(decimal? priceIn, decimal? priceOut)
        {
            if (priceIn > 0 && priceOut > 0)
            {
                return Math.Round((priceOut.Value - priceIn.Value) * 100m / priceOut.Value, 1);
            }

            return null;
        }
    }
}
