using System;

#pragma warning disable SA1402 // File may only contain a single class

namespace Telemart.Client.Dictionaries
{
    public abstract class OrderProductStatus : DictionaryItem, IComparable<OrderProductStatus>
    {
        protected const int NoneId = -1;
        protected const int NewId = 0;
        protected const int ClarifyId = 1;
        protected const int AgreedId = 2;
        protected const int DoneId = 6;

        protected OrderProductStatus(int id, string name)
            : base(id, name, true)
        {
        }

        public static OrderProductStatus None { get; } = new NoneOrderProductStatus();

        public static OrderProductStatus New { get; } = new NewOrderProductStatus();

        public static OrderProductStatus Clarify { get; } = new ClarifyOrderProductStatus();

        public static OrderProductStatus Agreed { get; } = new AggreedOrderProductStatus();

        public static OrderProductStatus Done { get; } = new DoneOrderProductStatus();

        public abstract bool CalcProductDateX { get; }

        public abstract bool CanBeRemoved { get; }

        public abstract bool AllowEditPrice { get; }

        public abstract bool AllowEditQuantity { get; }

        public abstract bool AllowSetSource { get; }

        public static bool operator <(OrderProductStatus s1, OrderProductStatus s2)
        {
            return s1.Id < s2.Id;
        }

        public static bool operator >(OrderProductStatus s1, OrderProductStatus s2)
        {
            return s1.Id > s2.Id;
        }

        public int CompareTo(OrderProductStatus other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return Id.CompareTo(other.Id);
        }
    }
    public sealed class NoneOrderProductStatus : OrderProductStatus
    {
        public NoneOrderProductStatus()
            : base(NoneId, string.Empty)
        {
        }

        public override bool CalcProductDateX { get; } = true;

        public override bool CanBeRemoved { get; } = true;

        public override bool AllowEditPrice { get; } = false;

        public override bool AllowEditQuantity { get; } = true;

        public override bool AllowSetSource { get; } = false;
    }

    public sealed class NewOrderProductStatus : OrderProductStatus
    {
        public NewOrderProductStatus()
            : base(NewId, "Новый")
        {
        }

        public override bool CalcProductDateX { get; } = true;

        public override bool CanBeRemoved { get; } = true;

        public override bool AllowEditPrice { get; } = true;

        public override bool AllowEditQuantity { get; } = true;

        public override bool AllowSetSource { get; } = false;
    }

    public sealed class ClarifyOrderProductStatus : OrderProductStatus
    {
        public ClarifyOrderProductStatus()
            : base(ClarifyId, "Уточнить")
        {
        }

        public override bool CalcProductDateX { get; } = true;

        public override bool CanBeRemoved { get; } = true;

        public override bool AllowEditPrice { get; } = true;

        public override bool AllowEditQuantity { get; } = true;

        public override bool AllowSetSource { get; } = true;
    }

    public sealed class AggreedOrderProductStatus : OrderProductStatus
    {
        public AggreedOrderProductStatus()
            : base(AgreedId, "Согласован")
        {
        }

        public override bool CalcProductDateX { get; } = true;

        public override bool CanBeRemoved { get; } = true;

        public override bool AllowEditPrice { get; } = true;

        public override bool AllowEditQuantity { get; } = false;

        public override bool AllowSetSource { get; } = true;
    }

    public sealed class DoneOrderProductStatus : OrderProductStatus
    {
        public DoneOrderProductStatus()
            : base(DoneId, "Выполнен")
        {
        }

        public override bool CalcProductDateX { get; } = false;

        public override bool CanBeRemoved { get; } = false;

        public override bool AllowEditPrice { get; } = false;

        public override bool AllowEditQuantity { get; } = false;

        public override bool AllowSetSource { get; } = false;
    }
}
