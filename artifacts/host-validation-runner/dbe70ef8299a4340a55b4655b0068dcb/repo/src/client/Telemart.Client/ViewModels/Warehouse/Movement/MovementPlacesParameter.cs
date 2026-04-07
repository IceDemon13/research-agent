namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class MovementPlacesParameter
    {
        public MovementPlacesParameter(int? places, int maxPlaces)
        {
            Places = places;
            MaxPlaces = maxPlaces;
        }

        public int? Places { get; }

        public int MaxPlaces { get; }
    }
}
