using System.Linq;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.EditForm;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public class UpdateFieldHelper : Behavior<ComboBoxEdit>
    {
        public string TargetField { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.EditValueChanged += AssociatedObject_EditValueChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.EditValueChanged -= AssociatedObject_EditValueChanged;
            base.OnDetaching();
        }

        private void AssociatedObject_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            EditFormControl editForm = LayoutTreeHelper.GetVisualParents(AssociatedObject).OfType<EditFormControl>().FirstOrDefault();

            ComboBoxEdit comboBoxEdit = LayoutTreeHelper.GetVisualChildren(editForm).OfType<ComboBoxEdit>().FirstOrDefault(elem => ((EditFormCellData)elem.DataContext).FieldName == TargetField);

            if (comboBoxEdit != null)
            {
                comboBoxEdit.EditValue = new DeliveryTypeDto();
                comboBoxEdit.Tag = "ValueChangedFromCode";
                comboBoxEdit.EditValue = null;
            }
        }
    }
}