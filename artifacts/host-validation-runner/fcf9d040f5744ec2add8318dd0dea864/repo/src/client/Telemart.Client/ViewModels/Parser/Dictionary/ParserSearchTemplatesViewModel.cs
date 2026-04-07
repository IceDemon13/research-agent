using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.ParserSearchTemplate;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.ParserSearchTemplate;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ParserSearchTemplatesViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper mapper;

        private readonly IErrorHandler errorHandler;

        private readonly IMessenger messenger;

        public ParserSearchTemplatesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            Title = "Шаблоны поиска по характеристикам";

            AddParserSearchTemplate = new DelegateCommand(Add, () => WebClient.IsOperationAllowed(BusinessOperation.ParserSearchTemplateUpdate));
            DeleteParserSearchTemplate = new AsyncCommand(DeleteAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ParserSearchTemplateUpdate) && SelectedParserSearchTemplate is not null);
            AddParserSearchTemplateFeature = new AsyncCommand(AddFeatureAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ParserSearchTemplateUpdate) && SelectedParserSearchTemplate is not null);
            DeleteParserSearchTemplateFeature = new AsyncCommand(DeleteFeatureAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ParserSearchTemplateUpdate) && SelectedParserSearchTemplateFeature is not null);

            this.messenger.Register<EntityMessage<ParserSearchTemplateDto>>(this, OnParserSearchTemplateMessage);
            this.messenger.Register<EntityMessage<ParserSearchTemplateFeatureDto>>(this, OnParserTemplateFeatureMessage);
        }

        #region Commands
        public IDelegateCommand AddParserSearchTemplate { get; }

        public IAsyncCommand DeleteParserSearchTemplate { get; }

        public IAsyncCommand AddParserSearchTemplateFeature { get; }

        public IAsyncCommand DeleteParserSearchTemplateFeature { get; }

        #endregion

        #region INPC

        public ObservableCollection<ParserSearchTemplateViewItem> ShowParserSearchTemplates
        {
            get { return GetProperty(() => ShowParserSearchTemplates); }
            set { SetProperty(() => ShowParserSearchTemplates, value); }
        }

        public ParserSearchTemplateViewItem SelectedParserSearchTemplate
        {
            get { return GetProperty(() => SelectedParserSearchTemplate); }
            set { SetProperty(() => SelectedParserSearchTemplate, value); }
        }

        public ParserSearchTemplateFeatureViewItem SelectedParserSearchTemplateFeature
        {
            get { return GetProperty(() => SelectedParserSearchTemplateFeature); }
            set { SetProperty(() => SelectedParserSearchTemplateFeature, value); }
        }

        public List<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public List<FeatureGroupDto> FeatureGroups
        {
            get { return GetProperty(() => FeatureGroups); }
            set { SetProperty(() => FeatureGroups, value); }
        }

        public List<GroupFeatureData> AllFeatures
        {
            get { return GetProperty(() => AllFeatures); }
            set { SetProperty(() => AllFeatures, value); }
        }

        #endregion

        public override int MinWidth { get; } = 700;

        public override int MinHeight { get; } = 350;

        public override int Width { get; } = 700;

        public override int Height { get; } = 350;

        protected override async Task HandleLoadedAsync()
        {
            List<ParserSearchTemplateDto> parserSearch = await WebClient.ExecuteApiRequestAsync(new QueryParserSearchTemplatesRequest());

            List<ParserSearchTemplateViewItem> listParserSearchItem = parserSearch.OrderBy(x => x.Name).Select(x => mapper.Map<ParserSearchTemplateViewItem>(x)).ToList();

            ShowParserSearchTemplates = listParserSearchItem.ToObservableCollection();

            FeatureGroups = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups()).GetPagedResultDataAsync();

            Categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            AllFeatures = FeatureGroups
                .SelectMany(x => x.Features
                    .Select(y => new GroupFeatureData()
                        {
                            FeatureId = y.Id,
                            FeatureName = y.Name,
                            GroupName = x.Name
                        })).ToList();

            if (ShowParserSearchTemplates.Count > 0)
            {
                SelectedParserSearchTemplate = ShowParserSearchTemplates[0];
            }

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
           CloseOk();
           return Task.CompletedTask;
        }

        private void Add()
        {
             DialogDocumentManagerService.ShowView<ParserSearchTemplateCreateViewModel>(null, this);
        }

        private async Task DeleteAsync()
        {
            if (MessageFacadeService.Confirm("Удалить шаблон?"))
            {
                int selectedId = SelectedParserSearchTemplate.Id;

                Result<object> result = await errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteParserSearchTemplateRequest(selectedId)), "удалении шаблона", "шаблон удален", this, true);
                if (result is not null)
                {
                    ShowParserSearchTemplates.Remove(SelectedParserSearchTemplate);

                    SelectedParserSearchTemplate = ShowParserSearchTemplates.FirstOrDefault();
                }
            }
        }

        private async Task AddFeatureAsync()
        {
            GetFeatureFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetFeatureFromUserViewModel>(new GetFeatureFromUserViewModelParameter("Выбор зависимой характеристики", SelectedParserSearchTemplate.CategoryId), this);

            if (viewModel.IsOk)
            {
                int? featureId = viewModel.Feature?.Id;

                if (featureId.HasValue)
                {
                    CreateParserSearchTemplateFeaturesRequest request = new CreateParserSearchTemplateFeaturesRequest(SelectedParserSearchTemplate.Id, featureId.Value);
                    Result<ParserSearchTemplateFeatureDto> result = await errorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(request), "выборе характеристики", "характеристика выбрана", this, true);

                    result.IfNotNull(_ =>
                    {
                        messenger.Send(new EntityMessage<ParserSearchTemplateFeatureDto>(result.Data, MessageType.Added));
                    });
                }
                else
                {
                    MessageFacadeService.ShowMessageBoxError("Не выбрана характеристика");
                }
            }
        }

        private async Task DeleteFeatureAsync()
        {
            if (MessageFacadeService.Confirm("Удалить характеристику?"))
            {
                int selectedFeatureId = SelectedParserSearchTemplateFeature.Id;
                Result<object> result = await errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteParserSearchTemplateFeaturesRequest(selectedFeatureId)), "удалении характеристики", "характеристика удалена", this, true);

                if (result is not null)
                {
                    SelectedParserSearchTemplate.Features.Remove(SelectedParserSearchTemplateFeature);

                    SelectedParserSearchTemplateFeature = SelectedParserSearchTemplate.Features.FirstOrDefault();
                }
            }
        }

        private void OnParserSearchTemplateMessage(EntityMessage<ParserSearchTemplateDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    ShowParserSearchTemplates.Add(mapper.Map<ParserSearchTemplateViewItem>(message.Entity));
                    break;
            }
        }

        private void OnParserTemplateFeatureMessage(EntityMessage<ParserSearchTemplateFeatureDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    ParserSearchTemplateFeatureViewItem newParserFeature = mapper.Map<ParserSearchTemplateFeatureViewItem>(message.Entity);
                    SelectedParserSearchTemplate.Features.Add(newParserFeature);
                    break;
            }
        }
    }

    public class GroupFeatureData
    {
        public int FeatureId { get; set; }

        public string FeatureName { get; set; }

        public string GroupName { get; set; }
    }
}