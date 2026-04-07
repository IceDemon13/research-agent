using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.Business.Delivery.Data
{
    public sealed class PackageProperties
    {
        public PackageProperties(IEnumerable<PackagePlaceProperties> places, bool fragile)
        {
            Places = places.ToArray();
            PlaceCount = Places.Count;
            Fragile = fragile;

            decimal totalWeight = 0;
            decimal totalInsurance = 0;

            foreach (PackagePlaceProperties place in Places)
            {
                totalWeight += place.Weight;
                totalInsurance += place.Insurance;
            }

            TotalWeight = totalWeight;
            TotalInsurance = totalInsurance;
        }

        public IReadOnlyCollection<PackagePlaceProperties> Places { get; }

        public int PlaceCount { get; }

        public decimal TotalWeight { get; }

        public decimal TotalInsurance { get; }

        public bool Fragile { get; }

        public bool AddToApplication { get; set; }
    }
}