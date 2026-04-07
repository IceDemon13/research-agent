using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class RecognizeBarcodeSettings
    {
        public RecognizeBarcodeSettings(
            bool allowEan13,
            bool allowOur,
            bool allowCode39 = false,
            bool includeSn = false,
            bool allowOurAssemblyService = false,
            IReadOnlyCollection<int> additionalServiceProductIds = null)
        {
            AllowCode39 = allowCode39;
            IncludeSn = includeSn;
            AllowEan13 = allowEan13;
            AllowOur = allowOur;
            AllowOurAssemblyService = allowOurAssemblyService;
            AllowOurAdditionalService = additionalServiceProductIds?.Any() == true;
            AdditionalServiceProductIds = additionalServiceProductIds;

            MinLength = 13;
            MaxLength = 13;

            if (allowCode39 || allowOur)
            {
                MinLength = 7;
                MaxLength = 15;
            }
        }

        public bool AllowCode39 { get; }

        public bool AllowEan13 { get; }

        public bool AllowOur { get; }

        public bool AllowOurAssemblyService { get; }

        public bool AllowOurAdditionalService { get; }

        public IReadOnlyCollection<int> AdditionalServiceProductIds { get; }

        public int MinLength { get; }

        public int MaxLength { get; }

        public bool IncludeSn { get; }
    }
}