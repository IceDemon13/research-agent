namespace Telemart.Client.ViewModels.Layouts
{
    public sealed class ModuleLayoutParameter
    {
        public ModuleLayoutParameter(int moduleId, bool isLoad, string layout, (string name, object value)[] parameters)
        {
            ModuleId = moduleId;
            IsLoad = isLoad;
            Parameters = parameters;
            Layout = layout;
        }

        public int ModuleId { get; }

        public bool IsLoad { get; }

        public string Layout { get; }

        public (string name, object value)[] Parameters { get; }
    }
}