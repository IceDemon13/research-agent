namespace Telemart.Client.Common.PrintSticker
{
    public class PrintStickerParameter
    {
        public PrintStickerParameter(int? carryId, bool showPreview)
        {
            CarryId = carryId;
            ShowPreview = showPreview;
        }

        public int? CarryId { get; }

        public bool ShowPreview { get; }
    }
}