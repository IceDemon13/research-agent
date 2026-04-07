using System.IO;
using OfficeOpenXml;
using Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill;
using Xunit;

namespace Telemart.Client.Tests.NovaposhtaBillTest
{
    public class NovapostaBillImportEngineTests
    {
        public NovapostaBillImportEngineTests()
        {
            ExcelPackage.License.SetNonCommercialOrganization("Not commercial");
        }

        [Fact]
        public void ImportTest1()
        {
            NovaposhtaBillExcellImportEngine engine = new NovaposhtaBillExcellImportEngine();

            var result = engine.ImportFromXlsx(Path.Combine("Data", "np_bill_short.xlsx"));

            Assert.Equal(35, result.ResultItems.Count);

            result = engine.ImportFromXlsx(Path.Combine("Data", "np_bill_long.xlsx"));

            Assert.Equal(893, result.ResultItems.Count);
        }
    }
}