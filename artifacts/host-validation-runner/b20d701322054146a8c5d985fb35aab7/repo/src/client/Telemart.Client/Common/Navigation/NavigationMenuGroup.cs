using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;

namespace Telemart.Client.Common.Navigation
{
    public sealed class NavigationMenuGroup : BindableBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationMenuGroup"/> class.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="items">The items.</param>
        /// <param name="isExpanded">Expanded group.</param>
        /// <exception cref="ArgumentNullException"><paramref name="header"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="isExpanded"/> is <see langword="null" />.</exception>
        public NavigationMenuGroup(string header, IEnumerable<NavigationMenuItem> items, bool isExpanded = true)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            IsExpanded = isExpanded;
            NavItems = new ObservableCollection<NavigationMenuItem>(items ?? throw new ArgumentNullException(nameof(items)));
        }

        public string Header
        {
            get { return GetProperty(() => Header); }
            set { SetProperty(() => Header, value); }
        }

        public bool IsExpanded
        {
            get { return GetProperty(() => IsExpanded); }
            set { SetProperty(() => IsExpanded, value); }
        }

        public ObservableCollection<NavigationMenuItem> NavItems
        {
            get { return GetProperty(() => NavItems); }
            set { SetProperty(() => NavItems, value); }
        }
    }
}