using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.FeatureGroup.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public class FeatureContractorParserSourcesViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper mapper;
        private readonly IErrorHandler errorHandler;

        private TelemartEnumerableCompareHelper<FeatureContractorParserSourceViewItem> compareHelper;

        public FeatureContractorParserSourcesViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.mapper = mapper;
            this.errorHandler = errorHandler;
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            private set { SetProperty(() => CategoryId, value); }
        }

        public ReadOnlyObservableCollection<FeatureContractorParserSourceViewItem> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public FeatureContractorParserSourceViewItem SelectedSource
        {
            get { return GetProperty(() => SelectedSource); }
            set { SetProperty(() => SelectedSource, value, SelectedSourceChanged); }
        }

        #region DialogSettings

        public override int Width => 700;

        public override int MinWidth => 500;

        public override int MaxWidth => 900;

        public override int Height => 500;

        public override int MinHeight => 400;

        public override int MaxHeight => 850;

        #endregion

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            FeatureContractorParserSourcesParameter parameter = (FeatureContractorParserSourcesParameter)Parameter;

            CategoryId = parameter.CategoryId;

            List<FeatureContractorParserSourceDto> featureContractorParserSources = await WebClient.ExecuteApiRequestAsync(new QueryFeatureContractorParserSource(CategoryId));

            Sources = featureContractorParserSources.Select(x => mapper.Map<FeatureContractorParserSourceViewItem>(x)).ToReadOnlyObservableCollection();

            Sources.ForEach(x => x.AllowedPriorities = Enumerable.Range(1, 100).Select(f => new ComboBoxItem(f, f.ToString())).ToReadOnlyObservableCollection());

            compareHelper = new TelemartEnumerableCompareHelper<FeatureContractorParserSourceViewItem>(Sources);

            await base.HandleLoadedAsync();

            Title = $"Настройка парсинга характеристик ({parameter.CategoryName})";
        }

        protected override async Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            FeatureContractorParserSourceSaveDto[] saveDtos = Sources.Select(x => new FeatureContractorParserSourceSaveDto(x.Id, x.Priority, x.FeatureId, x.ContractorId)).ToArray();

            (await errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateFeatureContractorParserSource(saveDtos)), "сохранении настроек", "Настройки сохранены", this, true))
                .IfNotNull(_ => CloseOk());
        }

        private void SelectedSourceChanged()
        {
            if (SelectedSource != null)
            {
                SelectedSource.AllowedPriorities = Enumerable.Range(1, 100)
                    .Except(Sources.Where(x => x.FeatureId == SelectedSource.FeatureId && x.ContractorId != SelectedSource.ContractorId).Select(x => x.Priority))
                    .Select(x => new ComboBoxItem(x, x.ToString()))
                    .ToReadOnlyObservableCollection();
            }
        }
    }
}