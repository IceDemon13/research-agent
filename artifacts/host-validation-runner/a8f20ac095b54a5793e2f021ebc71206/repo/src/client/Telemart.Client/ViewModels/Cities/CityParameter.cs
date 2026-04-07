using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cities
{
    public class CityParameter : EditorParameter
    {
        public CityParameter(int id)
            : base(id)
        {
        }

        public CityParameter(int id, CarryType carry, string districtRef, string areaRef)
            : base(id)
        {
            Carry = carry;
            DistrictRef = districtRef;
            AreaRef = areaRef;
        }

        public CarryType Carry { get; }

        public string DistrictRef { get; }

        public string AreaRef { get; }
    }
}