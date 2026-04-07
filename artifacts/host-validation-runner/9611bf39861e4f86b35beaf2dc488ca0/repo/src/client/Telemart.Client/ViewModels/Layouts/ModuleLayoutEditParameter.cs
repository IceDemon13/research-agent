namespace Telemart.Client.ViewModels.Layouts
{
    public sealed class ModuleLayoutEditParameter
    {
        public ModuleLayoutEditParameter(string label, int editorType, object value)
        {
            Label = label;
            EditorType = editorType;
            Value = value;
        }

        public string Label { get; set; }

        public int EditorType { get; set; }

        public object Value { get; set; }
    }
}