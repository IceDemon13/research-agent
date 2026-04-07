using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ValidationResultViewModel : TelemartDialogViewModelBase
    {
        public ValidationResultViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            HasErrors = true;
            Items = null;
        }

        public ValidationResultViewModel()
        {
        }

        public bool HasErrors
        {
            get { return GetProperty(() => HasErrors); }
            private set { SetProperty(() => HasErrors, value); }
        }

        public IReadOnlyCollection<ValidationResultItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public override int MinWidth => 600;

        public override int Width => 600;

        public override int MaxWidth => 1920;

        public override int MinHeight => 180;

        public override int Height => 400;

        public override int MaxHeight => 1080;

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

            ValidationResultViewModelParameter p = (ValidationResultViewModelParameter)parameter;

            Title = p.Title;
            Items = p.ValidationItems.ToArray();

            HasErrors = Items == null || Items.Any(x => x.IsError);
        }
    }
}
