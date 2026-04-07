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
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.Content.Actions;
using Telemart.Client.Data.Requests.Features.Products.Content;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.ProductsFeatures;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues
{
    public sealed class FeatureValuesViewModel : TelemartDialogViewModelBase<FeatureValuesParameter, IReadOnlyCollection<FeatureValueViewItem>>
    {
        public FeatureValuesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            ErrorHandler = errorHandler;
            Messenger = messenger;

            CreateValueCommand = new DelegateCommand(CreateValue, () => Parameter.FeatureId.HasValue || SelectedFeatureValue != null);
            DeleteValueCommand = new AsyncCommand(DeleteValueAsync, () => SelectedFeatureValue?.Id > 0);

            messenger.Register<EntityMessage<FeatureValueExDto>>(this, OnValueAction);
        }

        public FeatureValuesViewModel()
        {
        }

        public override int Width { get; } = 440;

        public override int Height { get; } = 600;

        public IDelegateCommand CreateValueCommand { get; }

        public IAsyncCommand DeleteValueCommand { get; }

        public ObservableCollection<FeatureValueViewItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            private set { SetProperty(() => FeatureValues, value); }
        }

        public FeatureValueViewItem SelectedFeatureValue
        {
            get { return GetProperty(() => SelectedFeatureValue); }
            set { SetProperty(() => SelectedFeatureValue, value); }
        }

        public ReadOnlyObservableCollection<FeatureFullDto> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; set; }

        private IMessenger Messenger { get; set; }

        protected override async Task HandleLoadedAsync()
        {
            FeatureFilteringItem featuresFilteringItem = new FeatureFilteringItem(Parameter.CategoryId);

            List<FeatureFullDto> features = await WebClient.ExecuteApiRequestAsync(new QueryFeatures(featuresFilteringItem)).GetPagedResultDataAsync();

            Features = features.ToReadOnlyObservableCollection();

            FeatureValues = Parameter.FeatureValues
                .OrderBy(x => x.Value)
                .ToObservableCollection();

            Title = "Значения характеристики";
        }

        protected override async Task HandleOkAsync()
        {
            if (FeatureValues.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationWarning("Заполните все значения характеристик валидными значениями");
                return;
            }

            FeatureValueSaveDto[] editedValues = FeatureValues
                .Where(x => x.IsChanged)
                .Select(x => Mapper.Map<FeatureValueSaveDto>(x))
                .ToArray();

            if (editedValues.Any())
            {
                if (!MessageFacadeService.Confirm("Вы уверены?"))
                {
                    return;
                }

                (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateFeatureValues(editedValues)), "обновлении значений", "Значения обновлены", this, true))
                    .IfNotNull(x =>
                    {
                        foreach (FeatureValueExDto featureValue in x.Data)
                        {
                            MessageType messageType = FeatureValues.Any(z => z.Id == featureValue.Id)
                                ? MessageType.Changed
                                : MessageType.Added;

                            Messenger.Send(new EntityMessage<FeatureValueExDto>(featureValue, messageType));

                            FeatureValues.RemoveAll(q => q.Id <= 0);

                            SetResult(FeatureValues);

                            CloseOk();
                        }
                    });
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");

                CloseOk();
            }
        }

        private async Task DeleteValueAsync()
        {
            if (SelectedFeatureValue?.Id > 0)
            {
                (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteFeatureProductValue(SelectedFeatureValue.Id)), "удалении значения", "Значение удалено", this, true))
                    .IfNotNull(_ => FeatureValues.Remove(SelectedFeatureValue));
            }
        }

        private void CreateValue()
        {
            int featureId = SelectedFeatureValue?.FeatureId ?? Parameter.FeatureId ?? 0;

            FeatureFullDto feature = Features.FirstOrDefault(x => x.Id == featureId);

            if (feature != null)
            {
                DialogDocumentManagerService.ShowView<CreateProductFeatureValueViewModel, CreateProductFeatureValueParameter, FeatureValueExDto>(new CreateProductFeatureValueParameter(feature.Id, feature.Regex, feature.MultiLanguage, false), this);
            }
        }

        private void OnValueAction(EntityMessage<FeatureValueExDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    FeatureValues.Add(Mapper.Map<FeatureValueViewItem>(message.Entity));

                    break;

                case MessageType.Changed:

                    FeatureValues.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));

                    break;
            }
        }
    }
}