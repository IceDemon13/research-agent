using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting.Native;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.Views.Common
{
    public partial class CategoryTreeView
    {
        public static readonly DependencyProperty CategoriesProperty = DependencyProperty.Register(
            nameof(Categories),
            typeof(ObservableCollection<CategoryViewItem>),
            typeof(CategoryTreeView),
            new PropertyMetadata(default(ObservableCollection<CategoryViewItem>)));

        public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty.Register(
            nameof(DisplayMember),
            typeof(string),
            typeof(CategoryTreeView),
            new PropertyMetadata(nameof(CategoryViewItem.Name)));

        public static readonly DependencyProperty FilterStringProperty = DependencyProperty.Register(
            nameof(FilterString),
            typeof(string),
            typeof(CategoryTreeView),
            new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IsTreeEnabledProperty = DependencyProperty.Register(
            nameof(IsTreeEnabled),
            typeof(bool),
            typeof(CategoryTreeView),
            new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedCategoriesProperty = DependencyProperty.Register(
            nameof(SelectedCategories),
            typeof(ObservableCollection<CategoryViewItem>),
            typeof(CategoryTreeView),
            new PropertyMetadata(default(ObservableCollection<CategoryViewItem>)));

        public static readonly DependencyProperty SelectedCategoryProperty = DependencyProperty.Register(
            nameof(SelectedCategory),
            typeof(CategoryViewItem),
            typeof(CategoryTreeView),
            new PropertyMetadata(default(CategoryViewItem)));

        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(MultiSelectMode),
            typeof(CategoryTreeView),
            new PropertyMetadata(MultiSelectMode.None));

        public static readonly DependencyProperty ShowBorderProperty = DependencyProperty.Register(
            nameof(ShowBorder),
            typeof(bool),
            typeof(CategoryTreeView),
            new PropertyMetadata(true));

        public static readonly DependencyProperty ShowLoadingPanelProperty = DependencyProperty.Register(
            nameof(ShowLoadingPanel),
            typeof(bool),
            typeof(CategoryTreeView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty AutoExpandAllNodesProperty = DependencyProperty.Register(
            nameof(AutoExpandAllNodes),
            typeof(bool),
            typeof(CategoryTreeView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ShowSearchPanelModeProperty = DependencyProperty.Register(
            nameof(ShowSearchPanelMode),
            typeof(ShowSearchPanelMode),
            typeof(CategoryTreeView),
            new PropertyMetadata(ShowSearchPanelMode.Always));

        public CategoryTreeView()
        {
            InitializeComponent();
        }

        public event EventHandler<CategoryViewItem> CurrentItemChanged;

        public event EventHandler<KeyEventArgs> TreeListViewPreviewKeyDown;

        public event EventHandler TreeListViewLoaded;

        public event EventHandler ItemsSourceChanged;

        public ObservableCollection<CategoryViewItem> Categories
        {
            get => (ObservableCollection<CategoryViewItem>)GetValue(CategoriesProperty);
            set => SetValue(CategoriesProperty, value);
        }

        public string DisplayMember
        {
            get => (string)GetValue(DisplayMemberProperty);
            set => SetValue(DisplayMemberProperty, value);
        }

        public string FilterString
        {
            get => (string)GetValue(FilterStringProperty);
            set => SetValue(FilterStringProperty, value);
        }

        public bool IsTreeEnabled
        {
            get => (bool)GetValue(IsTreeEnabledProperty);
            set => SetValue(IsTreeEnabledProperty, value);
        }

        public ObservableCollection<CategoryViewItem> SelectedCategories
        {
            get => (ObservableCollection<CategoryViewItem>)GetValue(SelectedCategoriesProperty);
            set => SetValue(SelectedCategoriesProperty, value);
        }

        public CategoryViewItem SelectedCategory
        {
            get => (CategoryViewItem)GetValue(SelectedCategoryProperty);
            set => SetValue(SelectedCategoryProperty, value);
        }

        public MultiSelectMode SelectionMode
        {
            get => (MultiSelectMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public bool ShowBorder
        {
            get => (bool)GetValue(ShowBorderProperty);
            set => SetValue(ShowBorderProperty, value);
        }

        public bool ShowLoadingPanel
        {
            get => (bool)GetValue(ShowLoadingPanelProperty);
            set => SetValue(ShowLoadingPanelProperty, value);
        }

        public bool AutoExpandAllNodes
        {
            get => (bool)GetValue(AutoExpandAllNodesProperty);
            set => SetValue(AutoExpandAllNodesProperty, value);
        }

        public ShowSearchPanelMode ShowSearchPanelMode
        {
            get => (ShowSearchPanelMode)GetValue(ShowSearchPanelModeProperty);
            set => SetValue(ShowSearchPanelModeProperty, value);
        }

        public void ExpandNodes(int tillLevel)
        {
            TreeListView.Nodes
                .Where(x => x.Level <= tillLevel)
                .ForEach(x => { x.IsExpanded = true; });
        }

        private void GridControlOnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            CurrentItemChanged?.Invoke(this, (CategoryViewItem)e.NewItem);
        }

        private void TreeListViewOnLoaded(object sender, RoutedEventArgs e)
        {
            TreeListViewLoaded?.Invoke(sender, e);
        }

        private void TreeListViewOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            TreeListViewPreviewKeyDown?.Invoke(sender, e);
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            ItemsSourceChanged?.Invoke(sender, EventArgs.Empty);
        }
    }
}
