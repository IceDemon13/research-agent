namespace Telemart.Client.ViewModels.Store
{
    public class PackagePropertiesProductParameter
    {
        public PackagePropertiesProductParameter(int? heigth, int? width, int? length)
        {
            Heigth = heigth;
            Width = width;
            Length = length;
        }

        public int? Heigth { get; }

        public int? Width { get; }

        public int? Length { get; }
    }
}