namespace Telemart.Client.ViewModels.Store
{
    public sealed class PackageMaxDimensionsParameter
    {
        public PackageMaxDimensionsParameter(int maxHeigth, int maxWidth, int maxLength)
        {
            MaxHeigth = maxHeigth;
            MaxWidth = maxWidth;
            MaxLength = maxLength;
        }

        public int MaxHeigth { get; }

        public int MaxWidth { get; }

        public int MaxLength { get; }
    }
}