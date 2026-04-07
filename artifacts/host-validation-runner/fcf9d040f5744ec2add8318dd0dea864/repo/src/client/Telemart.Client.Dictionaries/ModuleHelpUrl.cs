namespace Telemart.Client.Dictionaries
{
    public sealed class ModuleHelpUrl : DictionaryItem
    {
        public ModuleHelpUrl(int id, string moduleView, string moduleName, string url)
            : base(id, moduleView, true)
        {
            Url = url;
            ModuleName = moduleName;
        }
        
        public string ModuleName { get; }
        
        public string Url { get; }
    }
}