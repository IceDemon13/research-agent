using System;
using System.Linq;
using Telemart.Client.Core.Helpers;

namespace Telemart.Client.Reports.AssemblyService
{
    public class AssemblyServicePassportReportData
    {
        public AssemblyServicePassportReportData(
            string seriesAndModel,
            string nomenclatureSeries,
            DateTime completedOn,
            string lifeTime,
            string descriptionShort)
        {
            SeriesAndModel = seriesAndModel;
            NomenclatureSeries = nomenclatureSeries;
            CompletedOn = completedOn.ToString("dd.MM.yyyy");

            DescriptionShortHtml = $"<font size=2>{descriptionShort}<font/>";

            if (lifeTime.Contains("рок") || lifeTime.Contains("рік"))
            {
                string numberStr = lifeTime.Split(' ').First().Trim();

                if (int.TryParse(numberStr, out int number))
                {
                    number *= 12;

                    LifeTime = $"{number} {WordEndingHelper.GetWordByNumber(number, "місяць", "місяці", "місяців")}";
                }
                else
                {
                    LifeTime = lifeTime;
                }
            }
            else
            {
                LifeTime = lifeTime;
            }
        }

        public string SeriesAndModel { get; }

        public string NomenclatureSeries { get; }

        public string CompletedOn { get; }

        public string DescriptionShortHtml { get; }

        public string LifeTime { get; }
    }
}
