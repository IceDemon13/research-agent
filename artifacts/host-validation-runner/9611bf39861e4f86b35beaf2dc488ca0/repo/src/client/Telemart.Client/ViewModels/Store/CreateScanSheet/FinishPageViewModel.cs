using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Business.Delivery.ScanSheets;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.CreateScanSheet
{
    public sealed class FinishPageViewModel : WizardPageViewModelBase<CreateScanSheetModel>, ISupportWizardFinishCommand
    {
        public bool CanFinish => true;

        public override string Description
        {
            get
            {
                string descr = string.Empty;

                if (Model.Result != null)
                {
                    switch (Model.ScanSheetProcessorType)
                    {
                        case ScanSheetProcessorType.Novaposhta:
                            descr = Model.Result.Length > 1
                                ? "Реестры созданы и отправлены на печать"
                                : "Реестр создан и отправлен на печать";
                            break;
                        case ScanSheetProcessorType.MeestExpress:
                            descr = "Реестр создан и отправлен на печать";
                            break;
                        case ScanSheetProcessorType.Telemart:
                            descr = "Реестр создан";
                            break;
                    }
                }
                else
                {
                    descr = "Ошибки при создании реестра(ов)";
                }

                return descr;
            }
        }

        public override string Header { get; } = "Шаг 3 - Результат";

        public void OnFinish(CancelEventArgs e)
        {
        }
    }
}