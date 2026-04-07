using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace Telemart.Client.Reports.Product
{
    public partial class B2BBarcodeReport : XtraReport
    {
        public B2BBarcodeReport()
        {
            InitializeComponent();

            this.DataSourceRowChanged += B2BBarcodeReport_DataSourceRowChanged;
        }

        private void B2BBarcodeReport_DataSourceRowChanged(object sender, DataSourceRowEventArgs e)
        {
            AutoscaleControlText(xrLabel2, ((List<BarcodeReportData>)DataSource).First().ProductName);
        }

        private void AutoscaleControlText(XRControl control, string text)
        {
            var originalControlHeight = control.HeightF;

            if (control.Text == null || control.Text == string.Empty)
            {
                control.WidthF = 0;
                return;
            }

            float originalHeight = GetTextHeight(control, text);

            if (originalHeight > control.HeightF)
            {
                do
                {
                    control.Font = new DXFont(control.Font.Name, control.Font.Size - 0.1f, control.Font.Style);
                }
                while (control.HeightF - control.Padding.Top - control.Padding.Bottom < GetTextHeight(control, text));
            }
            else
            {
                do
                {
                    control.Font = new DXFont(control.Font.Name, control.Font.Size + 0.1f, control.Font.Style);
                }
                while (control.HeightF - control.Padding.Top - control.Padding.Bottom > GetTextHeight(control, text));
            }

            control.Font = new DXFont(control.Font.Name, control.Font.Size - 0.2f, control.Font.Style);
            control.HeightF = originalControlHeight;
        }

        private float GetTextHeight(XRControl label, string text)
        {

            DXStringFormat format = DXStringFormat.CreateGenericTypographic();
            format.FormatFlags = DXStringFormatFlags.FitBlackBox | DXStringFormatFlags.LineLimit | DXStringFormatFlags.NoClip;

            SizeF textSize = SizeF.Empty;
            float height = 0.0F;

            switch (ReportUnit)
            {
                case ReportUnit.HundredthsOfAnInch:
                    textSize = BrickGraphics.MeasureString(text, label.Font, (int)((label.Width - label.Padding.Left - label.Padding.Right) / 100), format, DXGraphicsUnit.Inch);
                    height = textSize.Height * 100;
                    break;

                case ReportUnit.TenthsOfAMillimeter:
                    textSize = BrickGraphics.MeasureString(text, label.Font, (int)((label.Width - label.Padding.Left - label.Padding.Right) / 10), format, DXGraphicsUnit.Millimeter);
                    height = textSize.Height * 10;
                    break;

                case ReportUnit.Pixels:
                    textSize = BrickGraphics.MeasureString(text, label.Font, (int)(label.Width - label.Padding.Left - label.Padding.Right), format, DXGraphicsUnit.Pixel);
                    height = textSize.Height;
                    break;
            }


            return height;
        }
    }
}
