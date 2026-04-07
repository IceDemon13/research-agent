using System;

namespace Telemart.Client.Dictionaries
{
    public abstract class DictionaryItemBase : IComparable<DictionaryItemBase>, IComparable, IDictionaryItem
    {
        protected DictionaryItemBase(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }

        public static bool operator !=(DictionaryItemBase left, DictionaryItemBase right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(DictionaryItemBase left, DictionaryItemBase right)
        {
            return Equals(left, right);
        }

        public virtual int CompareTo(DictionaryItemBase other)
        {
            return string.Compare(Name, other?.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((DictionaryItemBase)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as DictionaryItemBase);
        }

        public override string ToString()
        {
            return Name;
        }

        protected bool Equals(DictionaryItemBase other)
        {
            return other != null && Id == other.Id;
        }
    }
}