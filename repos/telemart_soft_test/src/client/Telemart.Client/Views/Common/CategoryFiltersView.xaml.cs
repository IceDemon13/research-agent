using System.Collections.ObjectModel;
using System.Windows;
using Telemart.Client.Controls.Accordion;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    ///     Interaction logic for CategoryFiltersView.xaml
    /// </summary>
    public partial class CategoryFiltersView
    {
        public static readonly DependencyProperty AccordionItemsProperty = DependencyProperty.Register(
            "AccordionItems",
            typeof(ObservableCollection<RootAccordionItem>),
            typeof(CategoryFiltersView),
            new PropertyMetadata(default(ObservableCollection<RootAccordionItem>)));

        public static readonly DependencyProperty IsSplashScreenShownProperty = DependencyProperty.Register(
            "IsSplashScreenShown",
            typeof(bool),
            typeof(CategoryFiltersView),
            new PropertyMetadata(default(bool)));

        public CategoryFiltersView()
        {
            InitializeComponent();
        }

        public bool IsSplashScreenShown
        {
            get => (bool)GetValue(IsSplashScreenShownProperty);
            set => SetValue(IsSplashScreenShownProperty, value);
        }

        public ObservableCollection<RootAccordionItem> AccordionItems
        {
            get => (ObservableCollection<RootAccordionItem>)GetValue(AccordionItemsProperty);
            set => SetValue(AccordionItemsProperty, value);
        }
    }
}