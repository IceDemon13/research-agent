using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class ChangeDateTimeViewModel : TelemartDialogViewModelBase<ChangeDateTimeParameter, ChangeDateTimeResult>
    {
        public ChangeDateTimeViewModel(
         IWebClient webClient,
         IDictionaries dictionaries,
         IMessageFacadeService messageFacadeService)
         : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public DateTime? OldDateTime
        {
            get { return GetProperty(() => OldDateTime); }
            set { SetProperty(() => OldDateTime, value); }
        }

        public DateTime? NewDateTime
        {
            get { return GetProperty(() => NewDateTime); }
            set { SetProperty(() => NewDateTime, value); }
        }

        public ObservableCollection<OrderStateChangeReasonViewItem> ChangeReasons
        {
            get { return GetProperty(() => ChangeReasons); }
            private set { SetProperty(() => ChangeReasons, value); }
        }

        public OrderStateChangeReasonViewItem CurrentChangeReason
        {
            get { return GetProperty(() => CurrentChangeReason); }
            set { SetProperty(() => CurrentChangeReason, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool ShowChangeReasons
        {
            get { return GetProperty(() => ShowChangeReasons); }
            set { SetProperty(() => ShowChangeReasons, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeDateTimeViewModel> builder)
        {
            builder.Property(x => x.NewDateTime).Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => y.Parameter?.IsValidFunc == null || string.IsNullOrEmpty(y.Parameter.IsValidFunc(x)), (x, y) => y.Parameter.IsValidFunc(x));
        }

        protected override async Task HandleLoadedAsync()
        {
            OldDateTime = Parameter.OldDateTime;
            Title = Parameter.Title;

            ShowChangeReasons = true;

            List<OrderStateChangeReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(false));

            ChangeReasons = reasons
                .Where(IsAllowOrderStateChangeReason)
                .Select(x => new OrderStateChangeReasonViewItem(x.Id, x.ParentId ?? 0, x.Name, x.Position))
                .ToObservableCollection();

            RaisePropertyChanged(nameof(NewDateTime));

            await base.HandleLoadedAsync();

            bool IsAllowOrderStateChangeReason(OrderStateChangeReasonDto dto)
            {
                if (Parameter.IsAdditionalDate)
                {
                    return dto.StateId == null || dto.OnlyAdditionalService;
                }

                if (Parameter.IsAssemblyServiceDate)
                {
                    return dto.StateId == null || dto.OnlyAssemblyService;
                }

                return dto.StateId == null || dto.StateId == Parameter.OrderStateId;
            }
        }

        protected override Task HandleOkAsync()
        {
            if (ShowChangeReasons)
            {
                if (CurrentChangeReason == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите причину");
                    return Task.CompletedTask;
                }

                if (ChangeReasons.Any(x => x.ParentId == CurrentChangeReason.Id))
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите причину, а не группу");
                    return Task.CompletedTask;
                }
            }

            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                SetResult(new ChangeDateTimeResult(NewDateTime!.Value, CurrentChangeReason?.Id, Comment));

                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}