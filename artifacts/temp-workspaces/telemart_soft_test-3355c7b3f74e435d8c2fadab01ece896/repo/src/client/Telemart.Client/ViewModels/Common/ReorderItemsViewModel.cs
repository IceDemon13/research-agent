using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ReorderItemsViewModel : TelemartDialogViewModelBase
    {
        private Func<IReadOnlyCollection<ComboBoxItem>, Task<bool>> okCommand;

        public ReorderItemsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            MoveUpCommand = new DelegateCommand<ComboBoxItem?>(MoveUp, x => x != null);
            MoveDownCommand = new DelegateCommand<ComboBoxItem?>(MoveDown, x => x != null);
            SortAscCommand = new DelegateCommand(SortAsc);
            SortDescCommand = new DelegateCommand(SortDesc);
        }

        public ReorderItemsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand MoveDownCommand { get; }

        public IDelegateCommand MoveUpCommand { get; }

        public IDelegateCommand SortAscCommand { get; }

        public IDelegateCommand SortDescCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ComboBoxItem> Items
        {
            get { return GetProperty(() => Items); }
            set { SetProperty(() => Items, value); }
        }

        public bool AllowSorting
        {
            get { return GetProperty(() => AllowSorting); }
            set { SetProperty(() => AllowSorting, value); }
        }

        #endregion

        protected override Task HandleLoadedAsync()
        {
            ReorderItemsParameter parameter = (ReorderItemsParameter)Parameter;

            Items = parameter.Items.ToObservableCollection();
            Title = parameter.Title;
            AllowSorting = parameter.AllowSorting;
            okCommand = parameter.OkCommand;

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                if (Items != null && okCommand != null)
                {
                    bool success = await okCommand(Items);

                    if (!success)
                    {
                        return;
                    }
                }

                IsOk = true;
                Close();
            }
        }

        private void MoveDown(ComboBoxItem? obj)
        {
            if (obj.HasValue)
            {
                int index = Items.IndexOf(obj.Value);

                if (index < Items.Count - 1)
                {
                    Items.Move(index, index + 1);
                }
            }
        }

        private void MoveUp(ComboBoxItem? obj)
        {
            if (obj.HasValue)
            {
                int index = Items.IndexOf(obj.Value);

                if (index > 0)
                {
                    Items.Move(index, index - 1);
                }
            }
        }

        private void SortAsc()
        {
            Items = new ObservableCollection<ComboBoxItem>(Items.OrderBy(x => x.DisplayValue));
        }

        private void SortDesc()
        {
            Items = new ObservableCollection<ComboBoxItem>(Items.OrderByDescending(x => x.DisplayValue));
        }
    }
}