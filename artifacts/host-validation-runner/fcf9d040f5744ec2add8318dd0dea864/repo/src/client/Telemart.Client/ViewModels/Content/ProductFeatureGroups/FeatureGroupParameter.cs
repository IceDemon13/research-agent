namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureGroupParameter
    {
        public FeatureGroupParameter(int id, string name, string nameUkr, string nameEn)
        {
            Id = id;
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
        }

        public int Id { get; }

        public string Name { get; }

        public string NameUkr { get; }

        public string NameEn { get; }
    }
}
