using System;

namespace Telemart.Client.Business.Parser
{
    public sealed class ComparsionItem : IEquatable<ComparsionItem>
    {
        public ComparsionItem(int stateId, int? productId, string displayName, ParserAliasState imageState)
        {
            StateId = stateId;
            ProductId = productId;
            DisplayName = displayName;
            ImageState = imageState;
        }

        public static ComparsionItem NotAssosiated { get; } = new ComparsionItem((int)ParserAliasState.NotAssosiated, null, string.Empty, ParserAliasState.NotAssosiated);

        public static ComparsionItem Postponed { get; } = new ComparsionItem((int)ParserAliasState.Postponed, null, "Отложен", ParserAliasState.Postponed);

        public static ComparsionItem Ignored { get; } = new ComparsionItem((int)ParserAliasState.Ignored, null, "Игнорируется", ParserAliasState.Ignored);

        public string DisplayName { get; }

        public ParserAliasState ImageState { get; }

        public int? ProductId { get; }

        public int StateId { get; }

        public static bool operator !=(ComparsionItem left, ComparsionItem right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ComparsionItem left, ComparsionItem right)
        {
            return Equals(left, right);
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

            return Equals((ComparsionItem)obj);
        }

        public bool Equals(ComparsionItem other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StateId == other.StateId && ProductId == other.ProductId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StateId * 397) ^ ProductId.GetHashCode();
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}