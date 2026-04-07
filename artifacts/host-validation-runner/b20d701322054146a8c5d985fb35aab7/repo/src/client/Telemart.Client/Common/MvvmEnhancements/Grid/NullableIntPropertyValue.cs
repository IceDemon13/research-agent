namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class NullableIntPropertyValue : PropertyValueBase<int?>
    {
        public NullableIntPropertyValue(int? value)
            : base(value)
        {
        }

        public override bool IsChangedToValue => Origin != null && Origin != 0 && Value != null && Value != 0 && !Value.Equals(Origin);

        public override bool IsChangedToEmpty => Origin != null && Origin != 0 && Value is null or 0;

        public override bool IsChangedFromEmpty => Origin is null or 0 && Value != null && Value != 0;
    }
}