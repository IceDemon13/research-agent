namespace Telemart.Client.Dictionaries
{
    public class AdditionalServicePriorityType : DictionaryItem
    {
        public const int BeforeAssemblyId = 1;
        public const int AfterAssemblyId = 2;

        public AdditionalServicePriorityType(int id, string name)
            : base(id, name, true)
        {
        }

        public static AdditionalServicePriorityType BeforeAssembly { get; } = new AdditionalServicePriorityType(BeforeAssemblyId, "До сборки");

        public static AdditionalServicePriorityType AfterAssembly { get; } = new AdditionalServicePriorityType(AfterAssemblyId, "После сборки");
    }
}
