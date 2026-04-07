using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KellermanSoftware.CompareNetObjects;

namespace Telemart.Client.Helpers
{
    public class TelemartCompareHelper<T> : TelemartCompareHelperBase
    where T : ICloneable
    {
        private T realTimeObj;
        private T originalObj;

        public TelemartCompareHelper(T originalObj, T realTimeObj, IEnumerable<string> membersToIgnore = null)
        : base(new CompareLogic(GetComparisonConfig(membersToIgnore)))
        {
            this.originalObj = originalObj;
            this.realTimeObj = realTimeObj;
        }

        public void Update(T updatedOriginalObj, T updatedRealTimeObj)
        {
            originalObj = updatedOriginalObj;
            realTimeObj = updatedRealTimeObj;
        }

        public override bool IsChanged()
        {
            return !CompareLogic.Compare(realTimeObj, originalObj).AreEqual;
        }
    }

    public class TelemartEnumerableCompareHelper<T> : TelemartCompareHelperBase
        where T : ICloneable
    {
        private IEnumerable<T> _realTimeObj;
        private IReadOnlyCollection<T> _originalObj;

        public TelemartEnumerableCompareHelper(IEnumerable<T> originalObj, IEnumerable<string> membersToIgnore = null)
        : base(new CompareLogic(GetComparisonConfig(membersToIgnore)))
        {
            UpdateObject(originalObj);
        }

        public void UpdateObject(IEnumerable<T> originalObj)
        {
            _originalObj = originalObj?.Select(x => (T)x.Clone()).ToArray() ?? Array.Empty<T>();
            _realTimeObj = originalObj ?? Enumerable.Empty<T>();
        }

        public override bool IsChanged()
        {
            if (_originalObj.Count != _realTimeObj.Count())
            {
                return true;
            }

            foreach (T item in _originalObj)
            {
                T realTimeItem = _realTimeObj.FirstOrDefault(x => CompareLogic.Compare(x, item).AreEqual);

                if (realTimeItem is null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public abstract class TelemartCompareHelperBase
    {
        protected TelemartCompareHelperBase(CompareLogic compareLogic)
        {
            CompareLogic = compareLogic;
        }

        public abstract bool IsChanged();

        protected CompareLogic CompareLogic { get; }

        protected static ComparisonConfig GetComparisonConfig(IEnumerable<string> userMembersToIgnore)
        {
            List<string> membersToIgnore = new List<string>
            {
                nameof(IDataErrorInfo.Error)
            };

            if (userMembersToIgnore?.Any() == true)
            {
                membersToIgnore.AddRange(userMembersToIgnore);
            }

            return new ComparisonConfig
            {
                CompareChildren = true,
                CompareFields = false,
                ComparePrivateFields = false,
                CompareProperties = true,
                CompareStaticProperties = false,
                CompareStaticFields = false,
                CompareReadOnly = true,
                ComparePrivateProperties = false,
                MembersToIgnore = membersToIgnore,
                AutoClearCache = false,
                IgnoreCollectionOrder = false
            };
        }
    }
}