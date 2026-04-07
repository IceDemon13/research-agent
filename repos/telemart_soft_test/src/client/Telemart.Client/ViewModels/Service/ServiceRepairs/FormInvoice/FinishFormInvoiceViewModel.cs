using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice
{
    public sealed class FinishFormInvoiceViewModel : WizardPageViewModelBase<FormInvoiceModel>, ISupportWizardFinishCommand
    {
        public FinishFormInvoiceViewModel(IMessenger messenger)
        {
            Messenger = messenger;
        }

        public bool CanFinish => true;

        public override string Description
        {
            get
            {
                string descr;

                if (Model.Result != null)
                {
                    if (Model.Result.Warnings == null || !Model.Result.Warnings.Any())
                    {
                        descr = "Накладная успешно сформирована";
                    }
                    else
                    {
                        descr = "Накладная сформирована с предупреждениями";
                    }
                }
                else
                {
                    descr = "Ошибки при формировании накладной";
                }

                return descr;
            }
        }

        public override string Header { get; } = "Шаг 4 - Результат";

        private IMessenger Messenger { get; }

        public void OnFinish(CancelEventArgs e)
        {
            if (Model.Result != null && Model.OpenInvoiceAfterCreation)
            {
                Messenger.Send(new ServiceInvoiceViewMessage(Model.Result.Data.Id));
            }
        }
    }
}
