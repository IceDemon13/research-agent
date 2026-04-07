using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.CustomerBonus;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public sealed class CustomerEditBonusesViewModel : TelemartDialogViewModelBase
    {
        private Func<(int bonusTypeId, int quantity, DateTime? burningDate), Task<bool>> okCommand;

        public CustomerEditBonusesViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService)
           : base(webClient, dictionaries, messageFacadeService)
        {
            Quantity = 1;
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public BonusType SelectedBonusType
        {
            get { return GetProperty(() => SelectedBonusType); }
            set { SetProperty(() => SelectedBonusType, value, ChangedBonusType); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public DateTime? ExpireDate
        {
            get { return GetProperty(() => ExpireDate); }
            set { SetProperty(() => ExpireDate, value); }
        }

        public bool ShowExpireDate
        {
            get { return GetProperty(() => ShowExpireDate); }
            set { SetProperty(() => ShowExpireDate, value); }
        }

        private IDispatcherService DispatcherService => GetService<IDispatcherService>();

        public static void BuildMetadata(MetadataBuilder<CustomerEditBonusesViewModel> builder)
        {
            builder.Property(x => x.SelectedBonusType)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => x < 100_000, () => "Максимальное значение 99999")
                .MatchesInstanceRule((x, y) => x > 0, () => "Минимальное значение 1");
        }

        protected override Task HandleLoadedAsync()
        {
            CustomerEditBonusesParameter parameter = (CustomerEditBonusesParameter)Parameter;
            BonusTypes = parameter.BonusTypes;
            SelectedBonusType = parameter.SelectedBonusType;
            Title = parameter.Title;
            ShowExpireDate = parameter.ShowExpireDate;
            okCommand = parameter.OkCommand;

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                if (okCommand != null)
                {
                    bool success = await okCommand((SelectedBonusType.Id, Quantity, ExpireDate));

                    if (!success)
                    {
                        return;
                    }
                }

                IsOk = true;
                Close();
            }
        }

        private void ChangedBonusType()
        {
            if (SelectedBonusType == null)
            {
                return;
            }

            Task.Factory.StartNew(
                async p =>
                {
                    try
                    {
                        ExpireDateDto dto = await WebClient.ExecuteApiRequestAsync(new QueryExpireDate((int)p, DateTime.Now));

                        await DispatcherService.BeginInvoke(
                            () =>
                            {
                                ExpireDate = dto.ExpireDate;
                            });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed getting expire date");
                    }
                },
                SelectedBonusType.Id,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}