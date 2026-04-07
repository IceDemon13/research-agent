namespace Telemart.Client.Dictionaries
{
    public sealed class FeatureOption : DictionaryItem
    {
        public const int PredetermineParentId = 3;

        public FeatureOption(int id, string name)
            : base(id, name, true)
        {
        }
    }
}