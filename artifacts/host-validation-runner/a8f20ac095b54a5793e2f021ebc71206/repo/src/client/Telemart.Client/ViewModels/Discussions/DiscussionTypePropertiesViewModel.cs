using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Common.CustomTypeDescriptors;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Discussions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public class DiscussionTypePropertiesViewModel : TelemartDialogViewModelBase
    {
        public DiscussionTypePropertiesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public CollectionTypeDescriptor PropertyGridSource
        {
            get { return GetProperty(() => PropertyGridSource); }
            set { SetProperty(() => PropertyGridSource, value); }
        }

        public string Body
        {
            get { return GetProperty(() => Body); }
            set { SetProperty(() => Body, value); }
        }

        public string FormatBody
        {
            get { return GetProperty(() => FormatBody); }
            set { SetProperty(() => FormatBody, value); }
        }

        public IReadOnlyCollection<DiscussionTypePropertyItem> PropertyItems
        {
            get { return GetProperty(() => PropertyItems); }
            set { SetProperty(() => PropertyItems, value); }
        }

        public override int Width => 700;

        public override int MinWidth => 700;

        public override int Height => 300;

        public override int MinHeight => 200;

        protected override async Task HandleLoadedAsync()
        {
            DiscussionTypeFillsParameter parameter = (DiscussionTypeFillsParameter)Parameter;

            PropertyItems = parameter.PropertyItems;

            IReadOnlyCollection<DiscussionTypePropertyItem> fills = await LoadPropertiesByDiscutionTypeAsync(parameter.DiscutionTypeId);

            PropertyGridSource = new CollectionTypeDescriptor(fills.OrderByDescending(x => x.Position).Select(MapToPropertyGridRow).ToArray());

            base.HandleLoadedAsync();

            Title = "Заполните поля";
        }

        protected override Task HandleOkAsync()
        {
            CollectionTypeDescriptor collectionTypeDescriptor = PropertyGridSource;

            IReadOnlyCollection<PropertyGridRow> rows = collectionTypeDescriptor.Rows;

            if (rows == null || rows.Any(x => x.Value is DiscussionTypePropertyItem item && string.IsNullOrEmpty(item.DiscussionText) && item.Required) == true)
            {
                MessageFacadeService.ShowMessageBoxWarning("Все обязательные поля должны быть заполенены");

                return Task.CompletedTask;
            }

            PropertyItems = rows.Select(x => x.Value as DiscussionTypePropertyItem).ToArray();

            Body = BodyGeneration(PropertyItems);
            FormatBody = BodyFormatGeneration(PropertyItems);

            CloseOk();

            return Task.CompletedTask;
        }

        private async Task<IReadOnlyCollection<DiscussionTypePropertyItem>> LoadPropertiesByDiscutionTypeAsync(int? discutionTypeId)
        {
            List<DiscussionTypePropertyItem> items = new List<DiscussionTypePropertyItem>();

            if (discutionTypeId.HasValue)
            {
                IReadOnlyCollection<DiscussionTypePropertyDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDiscussionTypeProprties(discutionTypeId.Value));

                items.AddRange(dtos.Select(x => new DiscussionTypePropertyItem(x.Id, x.NameUkr, x.Name, x.Required, x.Position, PropertyItems?.FirstOrDefault(y => y.Id == x.Id)?.DiscussionText)).ToArray());
            }

            if (items.Any() != true)
            {
                DiscussionTypePropertyItem itemDefault = new DiscussionTypePropertyItem(0, "Текст", "Текст", true, 100, PropertyItems?.FirstOrDefault()?.DiscussionText);

                items.Add(itemDefault);
            }

            return items;
        }

        private PropertyGridRow MapToPropertyGridRow(DiscussionTypePropertyItem propertyItem)
        {
            PropertyGridRow row = new PropertyGridRow(
                propertyItem.DisplayNameProperty,
                propertyItem.DisplayNameProperty,
                null,
                0,
                1,
                propertyItem,
                propertyItem.GetType(),
                false);

            return row;
        }


        private string BodyGeneration(IReadOnlyCollection<DiscussionTypePropertyItem> items)
        {
            StringBuilder builder = new StringBuilder(500);

            foreach (var item in items.OrderByDescending(x => x.Position).Where(x => !string.IsNullOrEmpty(x.DiscussionText)))
            {
                builder.Append(item.NameProperty).Append(": ").Append(item.DiscussionText).AppendLine().AppendLine();
            }

            return builder.ToString();
        }

        private string BodyFormatGeneration(IReadOnlyCollection<DiscussionTypePropertyItem> items)
        {
            const string TaskItemFormat = "[*][B]{0}: [/B]{1}";

            StringBuilder taskDescription = new StringBuilder(500);

            taskDescription.AppendLine("[LIST=1]");

            foreach (var item in items.OrderByDescending(x => x.Position).Where(x => !string.IsNullOrEmpty(x.DiscussionText)))
            {
                taskDescription.AppendFormat(CultureInfo.InvariantCulture, TaskItemFormat, item.NameProperty, item.DiscussionText).AppendLine().AppendLine();
            }

            taskDescription.AppendLine("[/LIST]");
            return taskDescription.ToString();
        }
    }
}