using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Tools.Tags
{
    public sealed class AutoFillOptionsViewModel : TelemartDialogViewModelBase
    {
        public AutoFillOptionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Печать ценников";
        }

        public AutoFillOptionsViewModel()
        {
        }

        public DateTime? FillAfterDateTime
        {
            get { return GetProperty(() => FillAfterDateTime); }
            set { SetProperty(() => FillAfterDateTime, value); }
        }

        public List<DateTime> LastPrintDateTimeValues
        {
            get { return GetProperty(() => LastPrintDateTimeValues); }
            set { SetProperty(() => LastPrintDateTimeValues, value); }
        }

        public ReadOnlyObservableCollection<TagFormat> TagFormats
        {
            get { return GetProperty(() => TagFormats); }
            set { SetProperty(() => TagFormats, value); }
        }

        public int? TagFormatId
        {
            get { return GetProperty(() => TagFormatId); }
            set { SetProperty(() => TagFormatId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AutoFillOptionsViewModel> builder)
        {
            builder.Property(x => x.FillAfterDateTime).Required();
        }

        protected override Task HandleLoadedAsync()
        {
            TagFormats = Dictionaries.GetItems<TagFormat>().ToReadOnlyObservableCollection();

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            LastPrintDateTimeValues = (List<DateTime>)parameter;
            FillAfterDateTime = LastPrintDateTimeValues.FirstOrDefault();
        }
    }
}
