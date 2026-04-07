using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Hashtag;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class SelectHashtagsViewModel : TelemartDialogViewModelBase
    {
        private Func<(List<int> plusHashTagIds, List<int> minusHashtagIds), Task<bool>> okCommand;

        public SelectHashtagsViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SelectedMinusHashtagIds = new ObservableCollection<int>();
            SelectedPlusHashtagIds = new ObservableCollection<int>();
        }

        public SelectHashtagsViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> MinusHashtags
        {
            get { return GetProperty(() => MinusHashtags); }
            private set { SetProperty(() => MinusHashtags, value); }
        }

        public ObservableCollection<int> SelectedMinusHashtagIds
        {
            get { return GetProperty(() => SelectedMinusHashtagIds); }
            set { SetProperty(() => SelectedMinusHashtagIds, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PlusHashtags
        {
            get { return GetProperty(() => PlusHashtags); }
            private set { SetProperty(() => PlusHashtags, value); }
        }

        public ObservableCollection<int> SelectedPlusHashtagIds
        {
            get { return GetProperty(() => SelectedPlusHashtagIds); }
            set { SetProperty(() => SelectedPlusHashtagIds, value); }
        }

        public bool MinusHashtagsEnabled
        {
            get { return GetProperty(() => MinusHashtagsEnabled); }
            set { SetProperty(() => MinusHashtagsEnabled, value); }
        }

        public bool PlusHashtagsEnabled
        {
            get { return GetProperty(() => PlusHashtagsEnabled); }
            set { SetProperty(() => PlusHashtagsEnabled, value); }
        }

        public bool MinusHashtagsRequired
        {
            get { return GetProperty(() => MinusHashtagsRequired); }
            set { SetProperty(() => MinusHashtagsRequired, value); }
        }

        public bool PlusHashtagsRequired
        {
            get { return GetProperty(() => PlusHashtagsRequired); }
            set { SetProperty(() => PlusHashtagsRequired, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SelectHashtagsViewModel> builder)
        {
            builder.Property(x => x.SelectedMinusHashtagIds)
                .MatchesInstanceRule((x, y) => !(!x.Any() && y.MinusHashtagsRequired), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedPlusHashtagIds)
               .MatchesInstanceRule((x, y) => !(!x.Any() && y.PlusHashtagsRequired), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            SelectHashtagsParameter parameter = (SelectHashtagsParameter)Parameter;

            Title = parameter.Title;
            MinusHashtagsEnabled = parameter.MinusHashtagsEnabled;
            PlusHashtagsEnabled = parameter.PlusHashtagsEnabled;
            MinusHashtagsRequired = parameter.MinusHashtagsRequired;
            PlusHashtagsRequired = parameter.PlusHashtagsRequired;
            okCommand = parameter.OkCommand;

            List<HashtagDto> hashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtags());

            MinusHashtags = hashtags
                  .Where(x => x.TypeId == HashtagType.Minus.Id && x.Active)
                  .Select(x => new ComboBoxItem(x.Id, x.Name))
                 .ToReadOnlyObservableCollection();

            PlusHashtags = hashtags
                 .Where(x => x.TypeId == HashtagType.Plus.Id && x.Active)
                 .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                bool success = await okCommand((SelectedPlusHashtagIds.ToList(), SelectedMinusHashtagIds.ToList()));

                if (!success)
                {
                    return;
                }

                IsOk = true;
                Close();
            }
        }
    }
}