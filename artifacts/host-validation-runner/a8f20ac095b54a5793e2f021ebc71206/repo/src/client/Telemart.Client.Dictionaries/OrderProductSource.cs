using System;

#pragma warning disable SA1402 // File may only contain a single class

namespace Telemart.Client.Dictionaries
{
    public abstract class OrderProductSource : DictionaryItem, IEquatable<OrderProductSource>
    {
        protected OrderProductSource(OrderProductSourceType sourceType)
            : base(sourceType.Id, sourceType.Name, true)
        {
            Real = sourceType.Real;
        }

        public virtual string SourceText => string.Empty;

        public virtual DateTime? SourceDate => null;

        public virtual int WarehouseId => 0;

        public bool Real { get; }

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

            return Equals((OrderProductSource)obj);
        }

        public bool Equals(OrderProductSource other)
        {
            return base.Equals(other) && other != null && WarehouseId == other.WarehouseId && SourceDate == other.SourceDate;
        }

        public override int GetHashCode()
        {
            int sourceDateHashCode = SourceDate?.GetHashCode() ?? 1;
            return base.GetHashCode() ^ WarehouseId ^ sourceDateHashCode;
        }

        public override string ToString()
        {
            return SourceText;
        }
    }

    public sealed class NoneOrderProductSource : OrderProductSource
    {
        public NoneOrderProductSource()
            : base(OrderProductSourceType.None)
        {
        }
    }

    public sealed class WarehouseOrderProductSource : OrderProductSource
    {
        public WarehouseOrderProductSource(int warehouseId, string sourceText, DateTime? sourceDate)
            : base(OrderProductSourceType.WarehouseSource)
        {
            SourceText = sourceText;
            WarehouseId = warehouseId;
            SourceDate = sourceDate;
        }

        public override DateTime? SourceDate { get; }

        public override string SourceText { get; }

        public override int WarehouseId { get; }
    }

    public sealed class PurchaseOrderProductSource : OrderProductSource
    {
        public PurchaseOrderProductSource(int warehouseId, string sourceText, DateTime? sourceDate)
            : base(OrderProductSourceType.Purchase)
        {
            WarehouseId = warehouseId;
            SourceText = sourceText;
            SourceDate = sourceDate;
        }

        public override int WarehouseId { get; }

        public override string SourceText { get; }

        public override DateTime? SourceDate { get; }
    }

    public sealed class MovementOrderProductSource : OrderProductSource
    {
        public MovementOrderProductSource(int warehouseId, string sourceText, DateTime? sourceDate)
            : base(OrderProductSourceType.Movement)
        {
            WarehouseId = warehouseId;
            SourceText = sourceText;
            SourceDate = sourceDate;
        }

        public override int WarehouseId { get; }

        public override string SourceText { get; }

        public override DateTime? SourceDate { get; }
    }

    public sealed class NoProductOrderProductSource : OrderProductSource
    {
        public NoProductOrderProductSource(string sourceText)
            : base(OrderProductSourceType.NoProduct)
        {
            SourceText = sourceText;
        }

        public override string SourceText { get; }
    }

    public sealed class OtherOrderProductSource : OrderProductSource
    {
        public OtherOrderProductSource(int warehouseId, string sourceText, DateTime? sourceDate)
            : base(OrderProductSourceType.Other)
        {
            WarehouseId = warehouseId;
            SourceText = sourceText;
            SourceDate = sourceDate;
        }

        public override int WarehouseId { get; }

        public override string SourceText { get; }

        public override DateTime? SourceDate { get; }
    }

    public sealed class LostOrderProductSource : OrderProductSource
    {
        public LostOrderProductSource(string sourceText)
            : base(OrderProductSourceType.Lost)
        {
            SourceText = sourceText;
        }

        public override string SourceText { get; }
    }

    public sealed class GuestOrderProductSource : OrderProductSource
    {
        public GuestOrderProductSource(int warehouseId, string sourceText, DateTime? sourceDate)
            : base(OrderProductSourceType.Guest)
        {
            SourceText = sourceText;
            WarehouseId = warehouseId;
            SourceDate = sourceDate;
        }

        public override string SourceText { get; }

        public override int WarehouseId { get; }

        public override DateTime? SourceDate { get; }
    }
}