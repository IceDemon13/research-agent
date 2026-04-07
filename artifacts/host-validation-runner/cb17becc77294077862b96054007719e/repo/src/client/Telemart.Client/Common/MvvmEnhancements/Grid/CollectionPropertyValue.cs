using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class CollectionPropertyValue : PropertyValueBase<List<object>>
    {
        public CollectionPropertyValue(List<object> value)
            : base(value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Origin = new List<object>(value);
        }

        public override bool IsChangedToValue => Origin?.Any() == true && Value?.Any() == true && !Value.ScrambledEquals(Origin);

        public override bool IsChangedToEmpty => Origin?.Any() == true && (Value == null || Value.Count == 0);

        public override bool IsChangedFromEmpty => (Origin == null || Origin.Count == 0) && Value?.Any() == true;

        public override bool IsEmpty()
        {
            return Value.Count == 0;
        }

        public void Clear()
        {
            Value?.Clear();

            RaisePropertiesChanged(nameof(Value), nameof(IsChangedToValue), nameof(IsChangedToEmpty), nameof(IsChangedFromEmpty), nameof(IsChanged));
        }

        public void Add(object val)
        {
            if (Value == null)
            {
                Value = new List<object>();
            }

            if (!Value.Contains(val))
            {
                Value.Add(val);

                RaisePropertiesChanged(nameof(Value), nameof(IsChangedToValue), nameof(IsChangedToEmpty), nameof(IsChangedFromEmpty), nameof(IsChanged));
            }
        }

        public bool ChangeValue(int fromValueId, int? toValueId)
        {
            bool changed = false;
            List<object> tmpValue = null;

            for (int i = 0; i < Value.Count; i++)
            {
                if (Value[i].Equals(fromValueId))
                {
                    tmpValue = new List<object>(Value);

                    if (toValueId.HasValue)
                    {
                        if (tmpValue.Contains(toValueId.Value))
                        {
                            tmpValue.RemoveAt(i);
                        }
                        else
                        {
                            tmpValue[i] = toValueId.Value;
                        }
                    }
                    else
                    {
                        tmpValue.RemoveAt(i);
                    }

                    break;
                }
            }

            if (tmpValue != null)
            {
                changed = true;
                Value = tmpValue;

                RaisePropertiesChanged(
                    nameof(Value),
                    nameof(IsChangedToValue),
                    nameof(IsChangedToEmpty),
                    nameof(IsChangedFromEmpty),
                    nameof(IsChanged));
            }

            return changed;
        }
    }
}