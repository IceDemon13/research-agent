using System;
using System.Linq.Expressions;
using System.Windows;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.Category
{
    public sealed class CategoryOptionViewModel<TOption> : ViewModelBase, ICategoryOption
    {
        private CategoryOverrideOption overrideOption;

        public CategoryOptionViewModel(
            CategoryFullDto parent,
            CategoryFullDto current,
            CategoryFullDto initial,
            Expression<Func<CategoryFullDto, TOption>> propertyGetter,
            Action<CategoryFullDto, TOption> propertySetter,
            string displayName,
            CategoryOptionInheritanceMode inheritanceMode,
            Func<TOption, string> displayValueGetter = null,
            bool isEditable = true,
            bool isVisible = true)
            : this()
        {
            Parent = parent;
            Current = current;
            Initial = initial;
            PropertyGetter = propertyGetter.Compile();
            PropertySetter = propertySetter;
            DisplayName = displayName;
            OptionName = GetOptionName(propertyGetter);
            DisplayValueGetter = displayValueGetter;
            IsEditable = isEditable;
            IsVisible = isVisible;
            InheritanceMode = inheritanceMode;

            CurrentValue = PropertyGetter(Current);
            ParentValue = PropertyGetter(Parent);
            InitialValue = PropertyGetter(Initial);

            OverrideOption = MapInheritanceModeToOverrideOption();
        }

        public CategoryOptionViewModel()
        {
            ResetCommand = new DelegateCommand(ResetChanges);
            SetParentValueCommand = new DelegateCommand(SetParentValue);
        }

        public TOption CurrentValue
        {
            get
            {
                return PropertyGetter(Current);
            }

            set
            {
                PropertySetter(Current, value);
                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(CurrentValue));
                RaisePropertyChanged(nameof(IsInherited));
                RaisePropertyChanged(nameof(IsCurrentValueEqualToParent));
                RaisePropertyChanged(nameof(IsCurrentValueChanged));
            }
        }

        public TOption InitialValue
        {
            get
            {
                return PropertyGetter(Initial);
            }

            set
            {
                PropertySetter(Initial, value);
                RaisePropertyChanged(nameof(Initial));
                RaisePropertyChanged(nameof(InitialValue));
            }
        }

        public TOption ParentValue
        {
            get
            {
                return PropertyGetter(Parent);
            }

            set
            {
                PropertySetter(Parent, value);
                RaisePropertyChanged(nameof(ParentValue));
            }
        }

        public bool IsEditable { get; }

        public bool IsVisible { get; }

        public string DisplayName { get; }

        public string OptionName { get; }

        public CategoryOptionInheritanceMode InheritanceMode { get; private set; }

        public bool IsInherited => Current.Level == 1 ||
            InheritanceMode == CategoryOptionInheritanceMode.NonInheritable ||
            ParentValue?.Equals(CurrentValue) == true;

        public bool IsCurrentValueEqualToParent =>
            InheritanceMode != CategoryOptionInheritanceMode.NonInheritable
            && InheritanceMode != CategoryOptionInheritanceMode.NonEditable
            && ParentValue?.Equals(CurrentValue) == true;

        public bool IsCurrentValueChanged =>
            !ReferenceEquals(null, CurrentValue) && !CurrentValue.Equals(InitialValue);

        public bool IsOverrideOptionEditable { get; private set; }

        public bool OverrideInAllDescendants
        {
            get { return GetProperty(() => OverrideInAllDescendants); }
            set { SetProperty(() => OverrideInAllDescendants, value); }
        }

        public bool OverrideOnlyInCategory
        {
            get { return GetProperty(() => OverrideOnlyInCategory); }
            set { SetProperty(() => OverrideOnlyInCategory, value); }
        }

        public bool IsEditableForCurrentUser
        {
            get { return GetProperty(() => IsEditableForCurrentUser); }
            set { SetProperty(() => IsEditableForCurrentUser, value); }
        }

        public CategoryOverrideOption OverrideOption
        {
            get
            {
                return overrideOption;
            }

            set
            {
                overrideOption = value;
                OverrideInAllDescendants = value == CategoryOverrideOption.OverrideInAllDescendants;
                OverrideOnlyInCategory = value == CategoryOverrideOption.OverrideOnlyInCategory;
                RaisePropertyChanged(nameof(OverrideOption));
            }
        }

        public object ParentDisplayValue
        {
            get
            {
                object value = DisplayValueGetter != null
                    ? (object)DisplayValueGetter(ParentValue)
                    : PropertyGetter(Parent);

                return value;
            }

            set
            {
                PropertySetter(Parent, (TOption)value);
                RaisePropertyChanged(nameof(ParentValue));
                RaisePropertyChanged(nameof(ParentDisplayValue));
            }
        }

        public object InitialDisplayValue
        {
            get
            {
                object value = DisplayValueGetter != null
                    ? (object)DisplayValueGetter(this.InitialValue)
                    : PropertyGetter(Initial);

                return value;
            }

            set
            {
                PropertySetter(Initial, (TOption)value);
                RaisePropertyChanged(nameof(InitialValue));
                RaisePropertyChanged(nameof(InitialDisplayValue));
            }
        }

        public object CurrentDisplayValue
        {
            get
            {
                object value = DisplayValueGetter != null
                    ? (object)DisplayValueGetter(this.CurrentValue)
                    : PropertyGetter(Current);

                return value;
            }

            set
            {
                PropertySetter(Current, (TOption)value);
                RaisePropertyChanged(nameof(CurrentValue));
                RaisePropertyChanged(nameof(CurrentDisplayValue));
            }
        }

        public IDelegateCommand ResetCommand { get; set; }

        public IDelegateCommand SetParentValueCommand { get; set; }

        object ICategoryOption.InitialValue => InitialValue;

        object ICategoryOption.CurrentValue => CurrentValue;

        private Func<TOption, string> DisplayValueGetter { get; }

        private CategoryFullDto Current { get; }

        private CategoryFullDto Initial { get; }

        private CategoryFullDto Parent { get; }

        private Func<CategoryFullDto, TOption> PropertyGetter { get; }

        private Action<CategoryFullDto, TOption> PropertySetter { get; }

        public void ResetChanges()
        {
            CurrentValue = InitialValue;
        }

        public void InitialValueChanged()
        {
            RaisePropertyChanged(nameof(InitialValue));
            RaisePropertyChanged(nameof(IsCurrentValueChanged));
        }

        private static bool ConfirmSetToParrent()
        {
            MessageBoxResult messageResult = DXMessageBox.Show(App.Current.MainWindow, "Вы уверены?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return messageResult == MessageBoxResult.Yes;
        }

        private static string GetOptionName(Expression<Func<CategoryFullDto, TOption>> propertyGetter)
        {
            MemberExpression expression = (MemberExpression)propertyGetter.Body;
            return expression.Member.Name;
        }

        private CategoryOverrideOption MapInheritanceModeToOverrideOption()
        {
            if (InheritanceMode == null)
            {
                IsOverrideOptionEditable = true;
                InheritanceMode = CategoryOptionInheritanceMode.InheritableToCategoryAndProduct;
                return CategoryOverrideOption.OverrideInDescendantsWithSameValue;
            }

            if (InheritanceMode == CategoryOptionInheritanceMode.NonInheritable)
            {
                return CategoryOverrideOption.OverrideOnlyInCategory;
            }

            if (InheritanceMode == CategoryOptionInheritanceMode.InheritableToCategory ||
                InheritanceMode == CategoryOptionInheritanceMode.InheritableToCategoryAndProduct)
            {
                IsOverrideOptionEditable = true;
                return CategoryOverrideOption.OverrideInDescendantsWithSameValue;
            }

            IsOverrideOptionEditable = true;
            return null;
        }

        private void SetParentValue()
        {
            if (ConfirmSetToParrent())
            {
                CurrentValue = ParentValue;
            }
        }
    }
}
