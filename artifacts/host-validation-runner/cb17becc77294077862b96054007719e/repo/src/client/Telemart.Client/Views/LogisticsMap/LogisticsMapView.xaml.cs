using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Diagram;
using Telemart.Client.ViewModels.LogisticsMap;

namespace Telemart.Client.Views.LogisticsMap
{
    public partial class LogisticsMapView : UserControl
    {
        private readonly LogisticsMapViewModel _viewModel;

        public LogisticsMapView()
        {
            InitializeComponent();

            _viewModel = (LogisticsMapViewModel)DataContext;

            _viewModel.OnDiagramShapeAdded += DiagramShapeAdded;
            _viewModel.OnDiagramConnectorAdded += DiagramConnectorAdded;
            _viewModel.OnDiagramCleared += DiagramCleared;
            _viewModel.PointerToolSelected += PointerToolSelected;
            _viewModel.PanToolSelected += PanToolSelected;

            diagramControl.ActiveTool = diagramControl.PointerTool;
        }

        private void DiagramShapeAdded(object sender, DiagramShape shape)
        {
            diagramControl.Items.Add(shape);
        }

        private void DiagramCleared(object sender, EventArgs args)
        {
            diagramControl.Items.Clear();
        }

        private void PanToolSelected(object sender, EventArgs args)
        {
            diagramControl.ActiveTool = diagramControl.PanTool;
        }

        private void PointerToolSelected(object sender, EventArgs args)
        {
            diagramControl.ActiveTool = diagramControl.PointerTool;
        }

        private void DiagramConnectorAdded(object sender, DiagramItem connector)
        {
            diagramControl.Items.Add(connector);
        }

        private void DiagramControl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DiagramShape shape = diagramControl.CalcHitItem(e.GetPosition(diagramControl)) as DiagramShape;

            _viewModel.DiagramDoubleClickCommand.Execute(shape);
        }

        private void DiagramControl_OnSelectionChanged(object sender, DiagramSelectionChangedEventArgs e)
        {
            _viewModel.DiagramSelectionChangedCommand.Execute(((DiagramControl)e.Source).SelectedItems?.FirstOrDefault());
        }
    }
}