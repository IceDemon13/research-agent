using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Core.IO;

namespace Telemart.Client.Business.Tag
{
    public abstract class TagPrinterBase : ITagPrinter
    {
        protected TagPrinterBase(string header, double templateWidth, double templateHeight)
        {
            Header = header ?? "Telemart.ua";
            TemplateWidth = templateWidth;
            TemplateHeight = templateHeight;
        }

        public string Header { get; }

        public double TemplateHeight { get; }

        public double TemplateWidth { get; }

        public Task PrintAsync(IReadOnlyCollection<TagPrintInfo> tagPrintInfos)
        {
            string html = BuildHtml(tagPrintInfos);
            return FileHelper.OpenAsFileAsync(Encoding.UTF8.GetBytes(html), "html");
        }

        protected abstract string BuildHtml(IReadOnlyCollection<TagPrintInfo> tagPrintInfos);

        protected int GetTitleFontSize()
        {
            return (int)Math.Round((TemplateWidth / 5.92) * 100);
        }
    }
}