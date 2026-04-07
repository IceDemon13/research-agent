namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public interface ITrackableValue
    {
        bool IsChanged { get; }

        bool IsEmpty();

        void ApplyChanges();
    }
}