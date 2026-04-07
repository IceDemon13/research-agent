using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.ParserSearchTemplate;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ParserSearchTemplate;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ParserSearchTemplateCreateViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler handler;
        private readonly IMessenger messenger;

        public ParserSearchTemplateCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            handler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public string NameTemplate
        {
            get { return GetProperty(() => NameTemplate); }
            set { SetProperty(() => NameTemplate, value); }
        }

        public int? SelectCategory
        {
            get { return GetProperty(() => SelectCategory); }
            set { SetProperty(() => SelectCategory, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSearchTemplateCreateViewModel> builder)
        {
            builder.Property(x => x.NameTemplate).
                MaxLength(100, () => "Максимальная длина названия шаблона 100 символов").
                Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectCategory).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories.Where(x => x.IsParent && x.Active > 0).OrderBy(x => x.FullName).ToReadOnlyObservableCollection();

            Title = "Создание шаблона для поиска";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            CreateParserSearchTemplateRequest request = new CreateParserSearchTemplateRequest(SelectCategory.Value, NameTemplate);

            Result<ParserSearchTemplateDto> result = await handler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(request), "создании шаблона", "шаблон создан", this, true);

            result.IfNotNull(_ =>
            {
                messenger.Send(new EntityMessage<ParserSearchTemplateDto>(result.Data, MessageType.Added));
                CloseOk();
            });
        }
    }
}