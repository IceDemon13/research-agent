using DevExpress.Xpf.Editors;

namespace Telemart.Client.Localizers
{
    public class TelemartEditorLocalizer : EditorResXLocalizer
    {
        protected override void PopulateStringTable()
        {
            base.PopulateStringTable();

            AddString(EditorStringId.TimePicker_ValidationErrorGreaterThan, "Значение должно быть больше чем {0:G}");
            AddString(EditorStringId.TimePicker_ValidationErrorLessThan, "Значение должно быть меньше чем {0:G}");
            AddString(EditorStringId.TimePicker_ValidationErrorInRange, "Значение должно быть между {0:G} и {1:G}");
        }
    }
}
