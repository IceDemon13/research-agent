using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Reporting.ViewItems;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportLegendViewModel : TelemartDialogViewModelBase
    {
        public ReportLegendViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Легенда";
        }

        public ReportLegendViewModel()
        {
        }

        #region display sizes

        public override int Width => 400;

        public override int MinWidth => 300;

        public override int Height => 600;

        public override int MinHeight => 400;

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ReportLegendViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        #endregion

        protected override Task HandleLoadedAsync()
        {
            Items = (Parameter as List<ReportLegendViewItem>).ToReadOnlyObservableCollection();
            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }
    }
}
