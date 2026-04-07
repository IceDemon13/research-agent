using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Telemart.Client.Business.Parser;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ParserAliasViewItemWrapper : IParserAliasViewItem, INotifyPropertyChanged
    {
        private readonly ParserAliasViewItem viewItem;

        public ParserAliasViewItemWrapper(
            ParserAliasViewItem viewItem,
            string categories,
            string contractors,
            IEnumerable<ContractorLinkViewItem> links)
        {
            this.viewItem = viewItem;
            Categories = categories;
            Contractors = contractors;
            FeatureName = viewItem.FeatureName;
            Links = new ObservableCollection<ContractorLinkViewItem>(links);

            this.viewItem.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public long Id
        {
            get { return viewItem.Id; }
            set { viewItem.Id = value; }
        }

        public int? ProductId
        {
            get { return viewItem.ProductId; }
            set { viewItem.ProductId = value; }
        }

        public int? NewProductId
        {
            get { return viewItem.NewProductId; }
            set { viewItem.NewProductId = value; }
        }

        public int StateId
        {
            get { return viewItem.StateId; }
            set { viewItem.StateId = value; }
        }

        public int NewStateId
        {
            get { return viewItem.NewStateId; }
            set { viewItem.NewStateId = value; }
        }

        public string Name
        {
            get { return viewItem.Name; }
            set { viewItem.Name = value; }
        }

        public string PartNumber
        {
            get { return viewItem.PartNumber; }
            set { viewItem.PartNumber = value; }
        }

        public DateTime CreatedOn
        {
            get { return viewItem.CreatedOn; }
            set { viewItem.CreatedOn = value; }
        }

        public DateTime? ModifiedOn
        {
            get { return viewItem.ModifiedOn; }
            set { viewItem.ModifiedOn = value; }
        }

        public int? ModifiedById
        {
            get { return viewItem.ModifiedById; }
            set { viewItem.ModifiedById = value; }
        }

        public string ModifiedByDisplayString
        {
            get { return viewItem.ModifiedByDisplayString; }
            set { viewItem.ModifiedByDisplayString = value; }
        }

        public DateTime? PostponedTo
        {
            get { return viewItem.PostponedTo; }
            set { viewItem.PostponedTo = value; }
        }

        public ComparsionResult ComparsionResult
        {
            get { return viewItem.ComparsionResult; }
            set { viewItem.ComparsionResult = value; }
        }

        public ObservableCollection<ComparsionItem> ComparsionItems
        {
            get { return viewItem.ComparsionItems; }
            set { viewItem.ComparsionItems = value; }
        }

        public ComparsionItem SelectedComparsionItem
        {
            get { return viewItem.SelectedComparsionItem; }
            set { viewItem.SelectedComparsionItem = value; }
        }

        public bool Edited => viewItem.Edited;

        public string Categories { get; set; }

        public string FeatureName { get; set; }

        public string Contractors { get; set; }

        public ObservableCollection<ContractorLinkViewItem> Links { get; set; }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}