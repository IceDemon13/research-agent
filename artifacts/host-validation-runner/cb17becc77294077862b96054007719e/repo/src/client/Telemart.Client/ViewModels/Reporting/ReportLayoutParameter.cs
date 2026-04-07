namespace Telemart.Client.ViewModels.Reporting
{
    public class ReportLayoutParameter
    {
        public ReportLayoutParameter(string label, int editorType, object value)
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