using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.ModuleAnalytics
{
    public class ModuleAnalyticsParameter
    {
        public ModuleAnalyticsParameter(
            int width,
            int height,
            int top,
            int left,
            string url,
            string title,
            int positionId,
            ISupportServices parentViewModel,
            int entityId,
            string viewModelName)
        {
            Width = width;
            Height = height;
            Url = url;
            Title = title;
            PositionId = positionId;
            ParentViewModel = parentViewModel;
            EntityId = entityId;
            ViewModelName = viewModelName;
            Top = top;
            Left = left;
        }

        public int Width { get; }

        public int Height { get; }

        public int Top { get; }

        public int Left { get; }

        public string Url { get; }

        public string Title { get; }

        public int PositionId { get; }

        public ISupportServices ParentViewModel { get; }

        public int EntityId { get; }

        public string ViewModelName { get; }
    }
}