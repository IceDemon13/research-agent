using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Layouts
{
    public interface IModuleLayoutService
    {
        public IDelegateCommand LoadLayoutCommand { get; }

        public IDelegateCommand SaveLayoutCommand { get; }

        public string Layout { get; set; }

        void LoadLayout(GridControl gridControl);

        void SaveLayout(GridControl gridControl);

        void Init(int moduleId, ISupportServices supportServices);
    }
}