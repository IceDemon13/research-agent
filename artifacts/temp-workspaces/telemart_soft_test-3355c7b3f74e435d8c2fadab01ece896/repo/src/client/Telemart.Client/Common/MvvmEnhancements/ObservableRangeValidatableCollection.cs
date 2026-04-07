using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Utils;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public sealed class ObservableRangeValidatableCollection<T> : ObservableRangeCollection<T>, IEnumerable<T>
        where T : ValidatableItem
    {
        private readonly Func<T, bool> isValidFunc;
        private readonly Func<IEnumerable<T>, IOrderedEnumerable<T>> orderItemsFunc;

        public ObservableRangeValidatableCollection(Func<T, bool> isValid, Func<IEnumerable<T>, IOrderedEnumerable<T>> orderItems = null)
        {
            isValidFunc = isValid;
            orderItemsFunc = orderItems;
        }

        public void Validate()
        {
            if (isValidFunc != null)
            {
                Items.ForEach(x => x.Valid = isValidFunc(x));

                if (orderItemsFunc != null)
                {
                    IEnumerable<T> items = orderItemsFunc(Items).ToArray();

                    Items.Clear();
                    items.ForEach(x => Items.Add(x));

                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }
    }
}
