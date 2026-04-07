using System.Collections.Generic;
using KellermanSoftware.CompareNetObjects;

namespace Telemart.Client.Common
{
    public class ObjectChangeTracker<TFirst, TSecond>
        where TFirst : class
        where TSecond : class
    {
        private CompareLogic compareLogic;

        public ObjectChangeTracker(TFirst first, TSecond second, List<string> membersToIgnore = null, List<string> membersToInclude = null)
        {
            First = first;
            Second = second;
            MembersToIgnore = membersToIgnore ?? new List<string>();
            MembersToInclude = membersToInclude ?? new List<string>();
        }

        public TFirst First { get; }

        public TSecond Second { get; }

        public bool IsChanged => !CompareLogic.Compare(First, Second).AreEqual;

        private List<string> MembersToIgnore { get; }

        private List<string> MembersToInclude { get; }

        private CompareLogic CompareLogic => compareLogic ?? (compareLogic = new CompareLogic(GetComparisonConfig()));

        private ComparisonConfig GetComparisonConfig()
        {
            return new ComparisonConfig
            {
                CompareChildren = true,
                IgnoreObjectTypes = true,
                CompareFields = false,
                ComparePrivateFields = false,
                CompareProperties = true,
                CompareStaticProperties = false,
                CompareStaticFields = false,
                CompareReadOnly = true,
                ComparePrivateProperties = false,
                SkipInvalidIndexers = true,
                MembersToIgnore = MembersToIgnore,
                MembersToInclude = MembersToInclude
            };
        }
    }
}
