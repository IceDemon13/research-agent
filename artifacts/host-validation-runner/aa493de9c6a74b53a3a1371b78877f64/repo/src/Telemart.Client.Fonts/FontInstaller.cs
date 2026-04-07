using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Telemart.Client.Core;

namespace Telemart.Client.Fonts
{
    public static class FontInstaller
    {
        public static void AddGeometryFontToCommonApplicationData()
        {
            string sourceFile = @"Geometria\Geometria.ttf";
            
            string destinationFile = Path.Combine(ApplicationFolders.CommonApplicationData, "Geometria.ttf");

            string[] filePaths = Directory.GetFiles(ApplicationFolders.CommonApplicationData);

            if (!filePaths.Any(x => x.Contains(FontNames.Geometria)))
            {
                File.Copy(sourceFile, destinationFile, false);
            }
        }
        
        public static int RegisterGeometriaFont()
        {
            string path = Path.Combine(ApplicationFolders.CommonApplicationData, "Geometria.ttf");
            
            return AddFontResourceExternal(path);
        }
        
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        private static extern int AddFontResourceExternal([In] [MarshalAs(UnmanagedType.LPWStr)] string fileName);
    }
}