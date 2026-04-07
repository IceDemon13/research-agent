using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;
using Telemart.Client.Business.Parser;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ParserAliasViewItem : BindableBase, IParserAliasViewItem
    {
        public long Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value, RaiseEditedPropertyChanged); }
        }

        public int? NewProductId
        {
            get { return GetProperty(() => NewProductId); }
            set { SetProperty(() => NewProductId, value, RaiseEditedPropertyChanged); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value, RaiseEditedPropertyChanged); }
        }

        public int NewStateId
        {
            get { return GetProperty(() => NewStateId); }
            set { SetProperty(() => NewStateId, value, RaiseEditedPropertyChanged); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
        }

        public string PartNumber
        {
            get { return GetProperty(() => PartNumber); }
            set { SetProperty(() => PartNumber, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int? ModifiedById
        {
            get { return GetProperty(() => ModifiedById); }
            set { SetProperty(() => ModifiedById, value); }
        }

        public string ModifiedByDisplayString
        {
            get { return GetProperty(() => ModifiedByDisplayString); }
            set { SetProperty(() => ModifiedByDisplayString, value); }
        }

        public DateTime? PostponedTo
        {
            get { return GetProperty(() => PostponedTo); }
            set { SetProperty(() => PostponedTo, value); }
        }

        public bool Edited => ProductId != NewProductId || StateId != NewStateId;

        public ComparsionResult ComparsionResult
        {
            get { return GetProperty(() => ComparsionResult); }
            set { SetProperty(() => ComparsionResult, value, ComparsionResultChangedCallback); }
        }

        public ObservableCollection<ComparsionItem> ComparsionItems
        {
            get { return GetProperty(() => ComparsionItems); }
            set { SetProperty(() => ComparsionItems, value); }
        }

        public ComparsionItem SelectedComparsionItem
        {
            get { return GetProperty(() => SelectedComparsionItem); }
            set { SetProperty(() => SelectedComparsionItem, value, SelectedComparsionItemChangedCallback); }
        }

        private void SelectedComparsionItemChangedCallback()
        {
            if (SelectedComparsionItem == null)
            {
                return;
            }

            NewStateId = SelectedComparsionItem.StateId;
            NewProductId = SelectedComparsionItem.ProductId;
        }

        private void ComparsionResultChangedCallback()
        {
            if (ComparsionResult == null)
            {
                return;
            }

            List<ComparsionItem> comparsionItems = ComparsionResult
                .ProposedValues
                .Select(x => new ComparsionItem((int)ParserAliasState.Associated, x.Id, x.Name, ParserAliasState.Associated))
                .Where(x => !ComparsionItems.Contains(x))
                .ToList();

            comparsionItems.AddRange(ComparsionItems);

            ComparsionItems = new ObservableCollection<ComparsionItem>(comparsionItems.OrderBy(x => x.StateId));
        }

        private void RaiseEditedPropertyChanged()
        {
            RaisePropertyChanged(nameof(Edited));
        }
    }
}