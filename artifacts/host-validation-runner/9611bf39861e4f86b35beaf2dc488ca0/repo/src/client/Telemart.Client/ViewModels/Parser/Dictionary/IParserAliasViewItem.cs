using System;
using System.Collections.ObjectModel;
using Telemart.Client.Business.Parser;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public interface IParserAliasViewItem
    {
        long Id { get; set; }

        int? ProductId { get; set; }

        int? NewProductId { get; set; }

        int StateId { get; set; }

        int NewStateId { get; set; }

        string Name { get; set; }

        string PartNumber { get; set; }

        DateTime CreatedOn { get; set; }

        DateTime? ModifiedOn { get; set; }

        int? ModifiedById { get; set; }

        DateTime? PostponedTo { get; set; }

        bool Edited { get; }

        ComparsionResult ComparsionResult { get; set; }

        ObservableCollection<ComparsionItem> ComparsionItems { get; set; }

        ComparsionItem SelectedComparsionItem { get; set; }
    }
}