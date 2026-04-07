using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class SegmentParameter : EditorParameter
    {
        public SegmentParameter(int id)
            : base(id)
        {
        }

        public bool AutoShowcase { get; } = true;
    }
}