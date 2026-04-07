using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Bitrix;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Bitrix;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class BitrixTaskViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, string> roles;

        public BitrixTaskViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
        }

        public ReadOnlyObservableCollection<BitrixCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public CategorizedBitrixTaskViewItem Task
        {
            get { return GetProperty(() => Task); }
            private set { SetProperty(() => Task, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            BitrixTaskParameter parameter = (BitrixTaskParameter)Parameter;

            roles = Dictionaries.GetItems<BitrixTaskRole>().ToDictionary(x => x.Id, y => y.Name);
            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();

            await RefreshCategories();

            Result<CategorizedBitrixTaskDto> result = await WebClient.ExecuteApiRequestAsync(new QueryBitrixTask(parameter.Id));

            Task = Mapper.Map<CategorizedBitrixTaskViewItem>(result.Data);

            Categories.CheckItems(Task.CategoryIds);

            SummaryItems = GetSummaryItems();

            Title = $"Задача №{Task.Id}";

            async Task RefreshCategories()
            {
                List<BitrixCategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryBitrixCategories());

                Categories = categories.Select(x => Mapper.Map<BitrixCategoryViewItem>(x)).ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(Task))
            {
                return;
            }

            int[] categoryIds = Categories.Where(x => x.Checked != false).Select(x => x.Id).ToArray();

            if (!categoryIds.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Ни одна категория не выбрана");
                return;
            }

            try
            {
                Result<CategorizedBitrixTaskDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateBitrixTask(
                    Task.Id,
                    Task.OurPriority.Id,
                    categoryIds));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Задача обновлена с ошибками";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Задача успешно обновлена");
                }

                Messenger.Send(new BitrixTaskMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении задачи");
                ShowValidationResultView("Ошибки при обновлении задачи", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update bitrix task");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении задачи");
                Logger.LogError(exception, "Error while updating bitrix task");
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Битрикс", Task.BitrixId.ToString());
            yield return new SummaryViewItem("Название", Task.Name);
            yield return new SummaryViewItem("Создал", $"{Task.CreatedByName} ({Task.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (Task.RoleId.HasValue)
            {
                yield return new SummaryViewItem("Роль", roles.GetValueOrDefault(Task.RoleId.Value));
            }
        }
    }
}