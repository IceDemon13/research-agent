using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.ModuleAnalytics;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.ModuleAnalytics
{
    public sealed class ModuleAnalyticsPositionViewModel : TelemartDialogViewModelBase
    {
        private readonly IModuleAnalyticsSettingsStore _moduleAnalyticsSettingsStore;
        private ModuleAnalyticsSettings _setings;
        private string _viewModelName;

        public ModuleAnalyticsPositionViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IModuleAnalyticsSettingsStore moduleAnalyticsSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _moduleAnalyticsSettingsStore = moduleAnalyticsSettingsStore;
        }

        public ComboBoxItem? SelectedLocation
        {
            get { return GetProperty(() => SelectedLocation); }
            set { SetProperty(() => SelectedLocation, value); }
        }

        public ComboBoxItem? SelectedPosition
        {
            get { return GetProperty(() => SelectedPosition); }
            set { SetProperty(() => SelectedPosition, value); }
        }

        public string ViewModelTitle
        {
            get { return GetProperty(() => ViewModelTitle); }
            set { SetProperty(() => ViewModelTitle, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Positions
        {
            get { return GetProperty(() => Positions); }
            private set { SetProperty(() => Positions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ModuleAnalyticsPositionViewModel> builder)
        {
            builder.Property(x => x.SelectedPosition)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedLocation)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ModuleAnalyticsPositionParameter parameter = (ModuleAnalyticsPositionParameter)Parameter;

            IReadOnlyCollection<Entity> entities = Dictionaries.GetItems<Entity>();

            if (string.IsNullOrWhiteSpace(parameter.ViewModelTitle))
            {
                Entity entity = entities.FirstOrDefault(x => x.DiscussionViewModels?.Any(z => z == ViewModelTitle) == true);

                if (entity is not null)
                {
                    ViewModelTitle = entity.DisplayName;
                }
                else
                {
                    ViewModelTitle = parameter.ViewModelName;
                }
            }
            else
            {
                ViewModelTitle = parameter.ViewModelTitle;
            }

            _setings = await _moduleAnalyticsSettingsStore.LoadAsync();
            _viewModelName = parameter.ViewModelName;

            Positions = Dictionaries
                .GetItems<ModuleAnalyticsPosition>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Locations = Dictionaries
                .GetItems<ModuleAnalyticsLocation>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            if (_setings.Settings.TryGetValue(_viewModelName, out ModuleAnalyticsSetting setting))
            {
                SelectedPosition = Positions.FirstOrDefault(x => x.Id == setting.PositionId);
            }

            if (_setings.Settings.TryGetValue(_viewModelName, out setting))
            {
                SelectedLocation = Locations.FirstOrDefault(x => x.Id == setting.LocationId);
            }

            if (SelectedPosition == null)
            {
                SelectedPosition = new ComboBoxItem(ModuleAnalyticsPosition.Maximized.Id, ModuleAnalyticsPosition.Maximized.Name);
            }

            if (SelectedLocation == null)
            {
                SelectedLocation = new ComboBoxItem(ModuleAnalyticsLocation.Horizontal.Id, ModuleAnalyticsLocation.Horizontal.Name);
            }

            Title = "Настройка формы аналитики";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            bool positionAlreadySaved = _setings.Settings.TryGetValue(_viewModelName, out ModuleAnalyticsSetting setting);

            if ((positionAlreadySaved && SelectedPosition?.Id == setting.PositionId && SelectedLocation?.Id == setting.LocationId)
                || (!positionAlreadySaved && SelectedPosition?.Id == ModuleAnalyticsPosition.Maximized.Id && SelectedLocation?.Id == ModuleAnalyticsLocation.Horizontal.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            _setings.Settings[_viewModelName] = new ModuleAnalyticsSetting
            {
                PositionId = SelectedPosition!.Value.Id,
                LocationId = SelectedLocation!.Value.Id
            };

            await _moduleAnalyticsSettingsStore.SaveAsync(_setings);

            CloseOk();
        }
    }
}