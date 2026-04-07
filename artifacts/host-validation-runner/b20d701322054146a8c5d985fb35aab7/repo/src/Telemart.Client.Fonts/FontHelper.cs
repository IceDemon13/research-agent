using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;

namespace Telemart.Client.Fonts
{
    public static class FontHelper
    {
        public static bool IsFontAlreadyInstalled(string fontName)
        {
            using InstalledFontCollection col = new InstalledFontCollection();

            List<string> fontNames = col.Families.Select(x => x.Name).ToList();

            return fontNames.Contains(fontName);
        }
    }
}