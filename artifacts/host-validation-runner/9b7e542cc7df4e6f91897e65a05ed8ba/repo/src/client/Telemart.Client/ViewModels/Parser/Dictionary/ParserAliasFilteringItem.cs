using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ParserAliasFilteringItem : IFilteringItem
    {
        public ParserAliasFilteringItem(
            string name,
            int? id,
            IReadOnlyCollection<ParserAliasStateDto> states,
            IReadOnlyCollection<int> categories,
            IReadOnlyCollection<int> contractors)
        {
            Name = name;
            Id = id;
            States = states;
            Categories = categories;
            Contractors = contractors;
        }

        public ParserAliasFilteringItem(IReadOnlyCollection<int> contractors)
        {
            Contractors = contractors;
        }

        public ParserAliasFilteringItem(int categoryId)
        {
            Categories = new[] { categoryId };
        }

        public IReadOnlyCollection<int> Categories { get; }

        public IReadOnlyCollection<int> Contractors { get; }

        public string Name { get; }

        public int? Id { get; }

        public IReadOnlyCollection<ParserAliasStateDto> States { get; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                yield return ("name", Name);
            }

            if (Id.HasValue)
            {
                yield return ("id", Id.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (States != null && States.Any())
            {
                string states = string.Join(",", States.Select(x => x.Id));
                yield return ("states", states);
            }

            if (Categories != null && Categories.Any())
            {
                string states = string.Join(",", Categories);
                yield return ("categories", states);
            }

            if (Contractors != null && Contractors.Any())
            {
                string states = string.Join(",", Contractors);
                yield return ("contractors", states);
            }
        }
    }
}