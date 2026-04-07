using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Telemart.Client.Common.Utils;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class ObservableRangeCollectionTests
    {
        private enum RangeAction
        {
            Add,
            Replace,
            Remove
        }

        [Fact]
        public void AddRangeTest()
        {
            int eventCollectionChangedCount = 0;
            int eventPropertyChangedCount = 0;

            ObservableRangeCollection<int> orc = new ObservableRangeCollection<int>(new List<int> { 0, 1, 2, 3 });
            orc.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
                eventCollectionChangedCount++;
            };
            ((INotifyPropertyChanged)orc).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, new[] { "Count", "Item[]" });
                eventPropertyChangedCount++;
            };

            orc.AddRange(new List<int> { 4, 5, 6, 7 });

            Assert.Equal(8, orc.Count);
            Assert.Equal(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 }, orc);
            Assert.Equal(1, eventCollectionChangedCount);
            Assert.Equal(2, eventPropertyChangedCount);
        }

        [Fact]
        public void ReplaceRangeTest()
        {
            int eventCollectionChangedCount = 0;
            int eventPropertyChangedCount = 0;

            ObservableRangeCollection<int> orc = new ObservableRangeCollection<int>(new List<int> { 0, 1, 2, 3 });
            orc.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
                eventCollectionChangedCount++;
            };
            ((INotifyPropertyChanged)orc).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, new[] { "Count", "Item[]" });
                eventPropertyChangedCount++;
            };

            orc.ReplaceRange(new List<int> { 4, 5, 6 });

            Assert.Equal(3, orc.Count);
            Assert.Equal(new List<int> { 4, 5, 6 }, orc);
            Assert.Equal(1, eventCollectionChangedCount);
            Assert.Equal(2, eventPropertyChangedCount);
        }

        [Fact]
        public void RemoveRangeTest()
        {
            int eventCollectionChangedCount = 0;
            int eventPropertyChangedCount = 0;

            ObservableRangeCollection<int> orc = new ObservableRangeCollection<int>(new List<int> { 0, 1, 2, 3 });
            orc.CollectionChanged += (sender, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
                eventCollectionChangedCount++;
            };
            ((INotifyPropertyChanged)orc).PropertyChanged += (sender, e) =>
            {
                Assert.Contains(e.PropertyName, new[] { "Count", "Item[]" });
                eventPropertyChangedCount++;
            };

            orc.RemoveRange(new List<int> { 1, 3, 6 });

            Assert.Equal(2, orc.Count);
            Assert.Equal(new List<int> { 0, 2 }, orc);
            Assert.Equal(1, eventCollectionChangedCount);
            Assert.Equal(2, eventPropertyChangedCount);
        }

        [Fact]
        public void AddEmptyRangeTest()
        {
            EmptyRangeTest(RangeAction.Add);
        }

        [Fact]
        public void ReplaceEmptyRangeTest()
        {
            EmptyRangeTest(RangeAction.Replace);
        }

        [Fact]
        public void RemoveEmptyRangeTest()
        {
            EmptyRangeTest(RangeAction.Remove);
        }

        [Fact]
        public void AddNullRangeTest()
        {
            Assert.Throws<ArgumentNullException>(() => { new ObservableRangeCollection<int>().AddRange(null); });
        }

        [Fact]
        public void ReplaceNullRangeTest()
        {
            Assert.Throws<ArgumentNullException>(() => { new ObservableRangeCollection<int>().ReplaceRange(null); });
        }

        [Fact]
        public void RemoveNullRangeTest()
        {
            Assert.Throws<ArgumentNullException>(() => { new ObservableRangeCollection<int>().RemoveRange(null); });
        }

        private void EmptyRangeTest(RangeAction action)
        {
            int eventCollectionChangedCount = 0;
            int eventPropertyChangedCount = 0;

            ObservableRangeCollection<int> orc = new ObservableRangeCollection<int>(new List<int> { 0, 1, 2, 3 });
            orc.CollectionChanged += (sender, e) => { eventCollectionChangedCount++; };
            ((INotifyPropertyChanged)orc).PropertyChanged += (sender, e) => { eventPropertyChangedCount++; };

            switch (action)
            {
                case RangeAction.Replace:
                    orc.ReplaceRange(new List<int>());
                    break;
                case RangeAction.Remove:
                    orc.RemoveRange(new List<int>());
                    break;
                default:
                    orc.AddRange(new List<int>());
                    break;
            }

            Assert.Equal(4, orc.Count);
            Assert.Equal(new List<int> { 0, 1, 2, 3 }, orc);
            Assert.Equal(0, eventCollectionChangedCount);
            Assert.Equal(0, eventPropertyChangedCount);
        }
    }
}