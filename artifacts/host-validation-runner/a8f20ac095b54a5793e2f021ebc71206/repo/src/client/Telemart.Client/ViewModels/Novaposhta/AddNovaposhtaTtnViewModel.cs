using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Novaposhta
{
    public sealed class AddNovaposhtaTtnViewModel : TelemartDialogViewModelBase
    {
        public AddNovaposhtaTtnViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public AddNovaposhtaTtnViewModel()
        {
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            private set { SetProperty(() => CarryType, value, () => { RaisePropertyChanged(nameof(Ttn)); }); }
        }

        public ReadOnlyObservableCollection<NpContractorDto> NpConractors
        {
            get { return GetProperty(() => NpConractors); }
            private set { SetProperty(() => NpConractors, value); }
        }

        public ReadOnlyObservableCollection<NovaposhtaTtnSource> TtnSoures
        {
            get { return GetProperty(() => TtnSoures); }
            private set { SetProperty(() => TtnSoures, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PayerTypes
        {
            get { return GetProperty(() => PayerTypes); }
            private set { SetProperty(() => PayerTypes, value); }
        }

        public string Ttn
        {
            get { return GetProperty(() => Ttn); }
            set { SetProperty(() => Ttn, value); }
        }

        public string ContractorRef
        {
            get { return GetProperty(() => ContractorRef); }
            set { SetProperty(() => ContractorRef, value); }
        }

        public int? TtnSourceId
        {
            get { return GetProperty(() => TtnSourceId); }
            set { SetProperty(() => TtnSourceId, value); }
        }

        public int? PayerTypeId
        {
            get { return GetProperty(() => PayerTypeId); }
            set { SetProperty(() => PayerTypeId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AddNovaposhtaTtnViewModel> builder)
        {
            builder.Property(x => x.Ttn)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryType.TtnRegex) || (x != null && Regex.IsMatch(x, y.CarryType.TtnRegex)),
                    () => "Введите корректно номер ТТН");

            builder.Property(x => x.ContractorRef)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TtnSourceId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PayerTypeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Comment)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            CarryType = Dictionaries.GetItemById<CarryType>(CarryType.NpWarehouseId);

            TtnSoures = Dictionaries.GetItems<NovaposhtaTtnSource>().ToReadOnlyObservableCollection();
            PayerTypes = GetPayerTypes().ToReadOnlyObservableCollection();

            List<NpContractorDto> npContractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(), true);
            NpConractors = npContractors.ToReadOnlyObservableCollection();

            Title = "Внесение ТТН";

            IEnumerable<ComboBoxItem> GetPayerTypes()
            {
                yield return new ComboBoxItem(NovaposhtaTtnPayer.Sender.Id, NovaposhtaTtnPayer.Sender.Name);
                yield return new ComboBoxItem(NovaposhtaTtnPayer.Recipient.Id, $"{NovaposhtaTtnPayer.Recipient.Name} (Telemart.ua)");
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<NpDocumentDto> result = await WebClient.ExecuteApiRequestAsync(new AddNpTtn(Ttn, ContractorRef, TtnSourceId.Value, PayerTypeId.Value, Comment));

                if (result.Warnings.Any())
                {
                    string message = "ТТН добавлена с предупреждениями";

                    ShowValidationResultView(
                        message,
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("ТТН успешно добавлена");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении ТТН");
                ShowValidationResultView("Ошибки при добавлении ТТН", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to add TTN");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении ТТН");
                Logger.LogError(exception, "Error while TTN adding");
            }
        }
    }
}