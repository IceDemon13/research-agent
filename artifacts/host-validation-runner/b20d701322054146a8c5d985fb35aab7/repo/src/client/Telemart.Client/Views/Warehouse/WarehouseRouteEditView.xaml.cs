using System;
using System.Windows;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Warehouse;

namespace Telemart.Client.Views.Warehouse
{
    /// <summary>
    /// Interaction logic for WarehouseRouteEditView.xaml
    /// </summary>
    public partial class WarehouseRouteEditView
    {
        private ComboBoxEdit comboBoxEdit;

        public WarehouseRouteEditView()
        {
            InitializeComponent();
        }

        private void OnValidateDeliveryType(object sender, GridCellValidationEventArgs e)
        {
            if (comboBoxEdit != null && (string)comboBoxEdit.Tag == "ValueChangedFromCode")
            {
                comboBoxEdit.Tag = null;
                e.IsValid = true;
                return;
            }

            WarehouseRouteTimeViewItem row = e.Row as WarehouseRouteTimeViewItem;

            if ((row.CarryId != CarryType.NpDeliveryId && row.CarryId != CarryType.NpWarehouseId && row.CarryId != CarryType.TeksId)
                || (e.Value is DeliveryTypeDto deliveryType
                    && (deliveryType.CarryId == row.CarryId || (row.CarryId == CarryType.TeksId && deliveryType.Id == (int)DeliveryType.DoorDoor))))
            {
                e.IsValid = true;
                e.ErrorContent = null;
            }
            else
            {
                e.IsValid = false;
                e.ErrorContent = "Тип доставки не соответствует выбранному способу";
            }
        }

        private void PART_Editor_OnLoaded(object sender, RoutedEventArgs e)
        {
            comboBoxEdit = sender as ComboBoxEdit;
        }
    }
}