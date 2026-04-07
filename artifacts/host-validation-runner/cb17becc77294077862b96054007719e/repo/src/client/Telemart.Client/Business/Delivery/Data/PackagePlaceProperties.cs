namespace Telemart.Client.Business.Delivery.Data
{
    public sealed class PackagePlaceProperties
    {
        public PackagePlaceProperties(decimal weight, decimal insurance, int? length = null, int? height = null, int? width = null)
        {
            Weight = weight;
            Insurance = insurance;
            Length = length;
            Height = height;
            Width = width;
        }

        public decimal Weight { get; }

        public decimal Insurance { get; }

        public int? Length { get; }

        public int? Height { get; }

        public int? Width { get; }
    }
}