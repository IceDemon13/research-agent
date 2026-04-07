using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.Content.Actions;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.FeatureImages;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureViewModel : TelemartEditorViewModelBase<FeatureFullDto, FeatureViewParameter, FeatureViewItem>
    {
        private int expectedPosition;

        public FeatureViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IFeatureIconValidator featureIconProcessor)
                : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            FeatureIconValidator = featureIconProcessor;

            RemoveFeatureKeyCommand = new DelegateCommand(RemoveFeatureKey, () => SelectedFeatureKey != null);
            AddFeatureKeyCommand = new DelegateCommand(AddFeatureKey);
            EditFeatureKeyCommand = new DelegateCommand(EditFeatureKey, () => SelectedFeatureKey != null);
            LoadFeatureImageCommand = new AsyncCommand(LoadFeatureImageAsync);
            ResetIconCommand = new AsyncCommand(ResetIconAsync, CanResetIcon);
            Images = new ObservableCollection<ImageSource>();
        }

        public FeatureViewModel()
        {
        }

        public IDelegateCommand RemoveFeatureKeyCommand { get; }

        public IDelegateCommand AddFeatureKeyCommand { get; }

        public IDelegateCommand EditFeatureKeyCommand { get; }

        public IAsyncCommand LoadFeatureImageCommand { get; }

        public IAsyncCommand ResetIconCommand { get; }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> FeatureGroups
        {
            get { return GetProperty(() => FeatureGroups); }
            set { SetProperty(() => FeatureGroups, value); }
        }

        public ReadOnlyObservableCollection<FeatureValueValidator> Validators
        {
            get { return GetProperty(() => Validators); }
            set { SetProperty(() => Validators, value); }
        }

        public FeatureValueValidator SelectedValidator
        {
            get { return GetProperty(() => SelectedValidator); }
            set { SetProperty(() => SelectedValidator, value, SelectedValidatorChanged); }
        }

        public FeatureContractorKeyViewItem SelectedFeatureKey
        {
            get { return GetProperty(() => SelectedFeatureKey); }
            set { SetProperty(() => SelectedFeatureKey, value); }
        }

        public ObservableCollection<ImageSource> Images
        {
            get { return GetProperty(() => Images); }
            private set { SetProperty(() => Images, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> FeatureOptions
        {
            get { return GetProperty(() => FeatureOptions); }
            private set { SetProperty(() => FeatureOptions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PredetermineFeatureValues
        {
            get { return GetProperty(() => PredetermineFeatureValues); }
            private set { SetProperty(() => PredetermineFeatureValues, value); }
        }

        public bool CommonFeatureSettingsOptionsVisibility => Model != null && Model.Settings;

        public bool FeatureDiscountSettingsOptionsVisibility => Model != null
                                                                    && Model.Settings
                                                                    && Model.FeatureOptionId == FeatureOption.PredetermineParentId;

        #endregion

        protected override string CreatedActionMessage => "Создана";

        protected override string EntityName => FeatureViewItem.TypeDisplayValue;

        protected override string UpdatedActionMessage => "Сохранена";

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        private IFeatureIconValidator FeatureIconValidator { get; }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            RaisePropertiesChanged(nameof(CommonFeatureSettingsOptionsVisibility), nameof(FeatureDiscountSettingsOptionsVisibility));
        }

        protected override Task<Result<FeatureFullDto>> CreateEntityAsync()
        {
            FeatureSaveDto dto = Mapper.Map<FeatureSaveDto>(Model);
            dto.Position = expectedPosition;
            return WebClient.ExecuteApiRequestAsync(new CreateFeature(dto));
        }

        protected override async Task<Result<FeatureFullDto>> UpdateEntityAsync()
        {
            FeatureSaveDto featureSaveDto = Mapper.Map<FeatureSaveDto>(Model);

            featureSaveDto.ChangeImage = Model.IconUrl != ModelOriginal.IconUrl && Model.FullNameIcon != ModelOriginal.FullNameIcon;

            if (featureSaveDto.ChangeImage == true && !string.IsNullOrEmpty(Model.FullNameIcon))
            {
                byte[] bytes = await FileHelper.ReadBytesAsync(Model.FullNameIcon);

                featureSaveDto.IconBytes = bytes;
            }

            return await WebClient.ExecuteApiRequestAsync(new UpdateFeature(Model.Id, featureSaveDto));
        }

        protected override object CreateEntityMessage(FeatureFullDto dto, MessageType messageType)
        {
            return new FeatureMessage(dto, messageType);
        }

        protected override Task<FeatureFullDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryFeature(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            FeatureViewParameter parameter = (FeatureViewParameter)Parameter;

            Validators = Dictionaries.GetItems<FeatureValueValidator>().ToReadOnlyObservableCollection();

            List<FeatureGroupDto> featureGroups = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups(parameter.CategoryId)).GetPagedResultDataAsync();

            FeatureGroups = featureGroups
                .OrderBy(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            expectedPosition = parameter.Position;

            await base.HandleLoadedAsync();

            await InitFeatureSettingsOptionsAsync(Model.Id);

            if (!string.IsNullOrEmpty(Model.IconUrl))
            {
                byte[] bytes = await FileHelper.ReadBytesFromUrlAsync(Model.IconUrl);

                Images.Clear();

                if (bytes != null)
                {
                    Images.Add(GetImage(bytes));
                }
            }
        }

        protected override Task<LockResponse<FeatureFullDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockFeature(id));
        }

        protected override Task<LockResponse<FeatureFullDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockFeature(id));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            foreach (string memberToIgnore in base.GetMembersToIgnore())
            {
                yield return memberToIgnore;
            }

            yield return nameof(FeatureViewItem.IsMultiValue);
            yield return nameof(FeatureViewItem.CustomRegex);
            yield return "Group";
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание характеристики";
        }

        protected override void SetEditTitle()
        {
            Title = $"{Model.Name} ({Model.Id})";
        }

        protected override void AfterSetData()
        {
            if (!string.IsNullOrWhiteSpace(Model.Regex))
            {
                SelectedValidator = Validators
                    .Where(x => x.Regex == Model.Regex)
                    .DefaultIfEmpty(FeatureValueValidator.RegexValidator)
                    .FirstOrDefault();

                Model.CustomRegex = SelectedValidator == FeatureValueValidator.RegexValidator;
            }

            RaisePropertiesChanged(nameof(CommonFeatureSettingsOptionsVisibility), nameof(FeatureDiscountSettingsOptionsVisibility));
        }

        private async Task InitFeatureSettingsOptionsAsync(int featureId)
        {
            List<FeatureOption> items = Dictionaries.GetItems<FeatureOption>().ToList();

            List<FeatureValueExDto> featureValues = WebClient.ExecuteApiRequest(new QueryFeatureValues(featureId));

            FeatureOptions = items
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            PredetermineFeatureValues = featureValues
                .OrderBy(x => x.Value)
                .Select(x => new ComboBoxItem(x.Id, x.Value))
                .ToReadOnlyObservableCollection();
        }

        private void SelectedValidatorChanged()
        {
            Model.CustomRegex = SelectedValidator == FeatureValueValidator.RegexValidator;

            if (!Model.CustomRegex)
            {
                Model.Regex = SelectedValidator?.Regex;
            }
        }

        private void AddFeatureKey()
        {
            FeatureContractorKeyViewModel viewModel = DialogDocumentManagerService.ShowView<FeatureContractorKeyViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Model.FeatureContractorKeys ??= new ObservableCollection<FeatureContractorKeyViewItem>();

            if (Model.FeatureContractorKeys.Any(x => x.ContractorId == viewModel.Contractor!.Value.Id && x.Key == viewModel.Key))
            {
                MessageFacadeService.ShowNotificationWarning("Такая настройка уже добавлена");
                return;
            }

            Model.FeatureContractorKeys.Add(new FeatureContractorKeyViewItem(0, viewModel.Contractor!.Value.Id, viewModel.Contractor.Value.DisplayValue, viewModel.Key));
        }

        private void EditFeatureKey()
        {
            FeatureContractorKeyViewModel viewModel = DialogDocumentManagerService.ShowView<FeatureContractorKeyViewModel>(SelectedFeatureKey, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.FeatureContractorKeys.Any(x => x.ContractorId == viewModel.Contractor!.Value.Id && x.Key == viewModel.Key && x != SelectedFeatureKey))
            {
                MessageFacadeService.ShowNotificationWarning("Такая настройка уже добавлена");
                return;
            }

            SelectedFeatureKey.ContractorId = viewModel.Contractor!.Value.Id;
            SelectedFeatureKey.ContractorName = viewModel.Contractor.Value.DisplayValue;
            SelectedFeatureKey.Key = viewModel.Key;
        }

        private void RemoveFeatureKey()
        {
            Model.FeatureContractorKeys.Remove(SelectedFeatureKey);
        }

        private bool CanResetIcon()
        {
            return IsLockedByCurrentEmployee
                   && Model != null
                   && Model.IconUrl != ModelOriginal.IconUrl
                   && Model.FullNameIcon != ModelOriginal.FullNameIcon;
        }

        private async Task ResetIconAsync()
        {
            Model.IconId = ModelOriginal.IconId;
            Model.IconUrl = ModelOriginal.IconUrl;
            Model.FullNameIcon = ModelOriginal.FullNameIcon;

            Images.Clear();

            if (!string.IsNullOrEmpty(Model.IconUrl))
            {
                byte[] bytes = await FileHelper.ReadBytesFromUrlAsync(Model.IconUrl);

                Images.Add(GetImage(bytes));
            }
        }

        private async Task LoadFeatureImageAsync()
        {
            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            Result result = FeatureIconValidator.ProcessValidateFile(OpenFileDialogService.File);

            if (!result.IsSuccess)
            {
                MessageFacadeService.ShowValidationResultView(
                        "Ошибки валидации иконки",
                        result.ErrorObj.Details?.Select(x => new ValidationResultItem(x.ErrorMessage, true)).ToArray(),
                        this);

                return;
            }

            Model.FullNameIcon = OpenFileDialogService.File.GetFullName();

            byte[] bytes = await FileHelper.ReadBytesAsync(Model.FullNameIcon);
            Model.IconUrl = null;
            Model.IconId = null;

            Images.Clear();

            if (bytes?.Length > 0)
            {
                Images.Add(GetImage(bytes));
            }
        }

        private BitmapImage GetImage(byte[] imageBytes)
        {
            BitmapImage image = new BitmapImage();

            using (Stream stream = new MemoryStream(imageBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            return image;
        }
    }
}