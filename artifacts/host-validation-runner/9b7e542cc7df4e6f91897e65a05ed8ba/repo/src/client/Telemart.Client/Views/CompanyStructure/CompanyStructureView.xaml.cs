using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Diagram;
using Telemart.Client.ViewModels.CompanyStructure;

namespace Telemart.Client.Views.CompanyStructure
{
    public partial class CompanyStructureView : UserControl
    {
        private readonly CompanyStructureViewModel _viewModel;

        public CompanyStructureView()
        {
            InitializeComponent();

            _viewModel = (CompanyStructureViewModel)DataContext;
            _viewModel.PointerToolSelected += PointerToolSelected;
            _viewModel.PanToolSelected += PanToolSelected;

            CompanyStructureDiagramControl.ActiveTool = CompanyStructureDiagramControl.PanTool;
        }

        private void PanToolSelected(object sender, EventArgs args)
        {
            CompanyStructureDiagramControl.ActiveTool = CompanyStructureDiagramControl.PanTool;
        }

        private void PointerToolSelected(object sender, EventArgs args)
        {
            CompanyStructureDiagramControl.ActiveTool = CompanyStructureDiagramControl.PointerTool;
        }

        private void CompanyStructureDiagramControl_OnSelectionChanged(object sender, DiagramSelectionChangedEventArgs e)
        {
            _viewModel.DiagramSelectionChangedCommand.Execute(((DiagramControl)e.Source).SelectedItems?.FirstOrDefault());
        }

        private void CompanyStructureDiagramControl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DiagramShape shape = CompanyStructureDiagramControl.CalcHitItem(e.GetPosition(CompanyStructureDiagramControl)) as DiagramShape;

            _viewModel.DiagramDoubleClickCommand.Execute(shape);
        }
    }
}