using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotPropertyViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string DisplayName
        {
            get { return GetProperty(() => DisplayName); }
            set { SetProperty(() => DisplayName, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value, TypeIdChanged); }
        }

        public string Regex
        {
            get { return GetProperty(() => Regex); }
            set { SetProperty(() => Regex, value, () => RaisePropertyChanged(ValuesText)); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public string GroupPosition
        {
            get { return GetProperty(() => GroupPosition); }
            set { SetProperty(() => GroupPosition, value); }
        }

        public string Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value); }
        }

        public ObservableCollection<RobotPropertyValueViewItem> Values
        {
            get { return GetProperty(() => Values); }
            set { SetProperty(() => Values, value, OnValuesChanged); }
        }

        public string ValuesText
        {
            get { return GetProperty(() => ValuesText); }
            set { SetProperty(() => ValuesText, value); }
        }

        public bool ValuesGridEnabled => TypeId == RobotPropertyType.List.Id || TypeId == RobotPropertyType.MultiList.Id;

        public bool IsRegexEnabled => TypeId == RobotPropertyType.Text.Id;

        public static void BuildMetadata(MetadataBuilder<RobotPropertyViewItem> builder)
        {
            const int descriptionMaxLenght = 500;

            builder.Property(x => x.Regex)
                .MaxLength(90)
                .MatchesInstanceRule((x, y) => y.TypeId != RobotPropertyType.Text.Id || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.DisplayName)
                .MaxLength(90)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.GroupName)
                .MaxLength(40)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TypeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Description).MaxLength(descriptionMaxLenght, _ => $"Допустимое количество {descriptionMaxLenght} символов");
            builder.Property(x => x.Name)
                .MaxLength(90)
                .Required(() => Resources.RequiredErrorMessage)
                .LatinOrNumber();
        }

        public override object Clone()
        {
            RobotPropertyViewItem item = ReflectionObjectCloner.Clone(this);

            item.Values = Values?.Select(x =>
            {
                RobotPropertyValueViewItem viewItem = ReflectionObjectCloner.Clone(x);
                return viewItem;
            }).ToObservableCollection();

            return item;
        }

        private void TypeIdChanged()
        {
            RaisePropertiesChanged(nameof(ValuesGridEnabled), nameof(Regex), nameof(IsRegexEnabled));

            if (!IsRegexEnabled)
            {
                Regex = null;
            }

            if (!ValuesGridEnabled)
            {
                Values?.Clear();
            }
        }

        private void OnValuesChanged()
        {
            if (Values is null)
            {
                return;
            }

            ValuesText = Values.Any() ? string.Join(", ", Values.Select(x => x.DisplayName)) : $"Шаблон: {(TypeId == RobotPropertyType.Text.Id ? Regex : "да/нет")}";
        }
    }
}