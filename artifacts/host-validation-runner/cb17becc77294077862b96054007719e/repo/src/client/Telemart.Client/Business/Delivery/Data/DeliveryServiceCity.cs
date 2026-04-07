namespace Telemart.Client.Business.Delivery.Data
{
    public class DeliveryServiceCity
    {
        public DeliveryServiceCity(string cityRef, string name, string nameUa, bool active)
        {
            CityRef = cityRef;
            Name = name;
            NameUa = nameUa;
            Active = active;
        }

        public DeliveryServiceCity(string cityRef, string name, string nameUa, bool active, string areaRef, string districtRef)
            : this(cityRef, name, nameUa, active)
        {
            AreaRef = areaRef;
            DistrictRef = districtRef;
        }

        public string CityRef { get; }

        public string Name { get; }

        public string NameUa { get; }

        public bool Active { get; }

        public string AreaRef { get; }

        public string DistrictRef { get; }
    }
}