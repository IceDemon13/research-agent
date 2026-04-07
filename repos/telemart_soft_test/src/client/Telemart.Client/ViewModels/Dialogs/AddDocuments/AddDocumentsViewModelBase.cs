using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs.AddDocuments
{
    public abstract class AddDocumentsViewModelBase : TelemartDialogViewModelBase
    {
        protected AddDocumentsViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        protected AddDocumentsViewModelBase()
        {
        }

        public ReadOnlyObservableCollection<AddDocumentViewItem> Files
        {
            get { return GetProperty(() => Files); }
            set { SetProperty(() => Files, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value, () => ShowTypes = value?.Any() == true); }
        }

        public bool ShowTypes
        {
            get { return GetProperty(() => ShowTypes); }
            private set { SetProperty(() => ShowTypes, value); }
        }

        public AddDocumentsParameter ViewModelParameter
        {
            get { return GetProperty(() => ViewModelParameter); }
            private set { SetProperty(() => ViewModelParameter, value); }
        }

        public override int MinHeight => 200;

        public override int Height => 300;

        public override int MaxHeight => 400;

        public override int MinWidth => 500;

        public override int Width => 500;

        public override int MaxWidth => 800;

        protected override Task HandleLoadedAsync()
        {
            ViewModelParameter = (AddDocumentsParameter)Parameter;

            Types = GetTypes().ToReadOnlyObservableCollection();

            if (ViewModelParameter.Document is null)
            {
                Files = ViewModelParameter.Files.Select(x => new AddDocumentViewItem(x)).ToReadOnlyObservableCollection();
            }
            else
            {
                Files = new[] { new AddDocumentViewItem(ViewModelParameter.Document) }.ToReadOnlyObservableCollection();
            }

            if (Types.Any())
            {
                Files.ForEach(x => x.ShowTypes = true);
            }

            Title = ViewModelParameter.Title ?? "Загрузка документов";

            return Task.CompletedTask;
        }

        protected override bool CanOk()
        {
            return Files != null && !Files.Any(x => IDataErrorInfoHelper.HasErrors(x));
        }

        protected override async Task HandleOkAsync()
        {
            List<AddDocumentViewItem> notProcessedItems = Files.Where(x => x.IsError || !x.IsProcessed).ToList();

            if (!Validate(notProcessedItems))
            {
                return;
            }

            await Task.WhenAll(notProcessedItems.Select(ProcessItemAsync));

            if (notProcessedItems.All(x => x.IsSuccess))
            {
                MessageFacadeService.ShowNotificationInfo("Документы успешно добавлены");
                IsOk = true;
                Close();
            }
        }

        protected abstract IEnumerable<ComboBoxItem> GetTypes();

        protected abstract Task CreateDocumentAsync(int entityId, AddDocumentViewItem item);

        protected virtual bool Validate(IReadOnlyCollection<AddDocumentViewItem> notProcessedItems) => true;

        private async Task ProcessItemAsync(AddDocumentViewItem item)
        {
            try
            {
                await CreateDocumentAsync(ViewModelParameter.DocumentId, item);
            }
            catch (UnexpectedSatusException exception)
            {
                item.ErrorMessage = string.Join(Environment.NewLine, exception.GetErrorItems().Select(x => x.Message));
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create document");
                item.ErrorMessage = Resources.ServerConnectError;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while creating document");
                item.ErrorMessage = "Ошибка при создании документа";
            }
            finally
            {
                item.IsProcessed = true;
            }
        }
    }
}