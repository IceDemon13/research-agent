using System;
using System.IO;
using System.Xml;
using DevExpress.Xpf.LayoutControl;
using Telemart.Client.Core;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class FlowLayoutControlLayoutSerializationBehavior : BehaviorBase<FlowLayoutControl>
    {
        public string FileName { get; set; }

        protected override void OnSetup()
        {
            string filePath = GetFilePath(FileName);

            if (File.Exists(filePath))
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    using (XmlReader reader = XmlReader.Create(fileStream))
                    {
                        AssociatedObject.ReadFromXML(reader);
                    }
                }
            }

            AssociatedObject.LayoutUpdated += AssociatedObjectOnLayoutUpdated;
        }

        protected override void OnCleanup()
        {
            AssociatedObject.LayoutUpdated -= AssociatedObjectOnLayoutUpdated;

            SaveLayout();
        }

        private static void AssociatedObjectOnLayoutUpdated(object sender, EventArgs e)
        {
        }

        private static string GetFilePath(string fileName)
        {
            string directoryPath = Path.Combine(ApplicationFolders.LocalApplicationData, "layouts");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Path.Combine(directoryPath, fileName);
        }

        private void SaveLayout()
        {
            string filePath = GetFilePath(FileName);

            using (FileStream fileStream = File.OpenWrite(filePath))
            {
                using (XmlWriter writer = XmlWriter.Create(fileStream))
                {
                    AssociatedObject.WriteToXML(writer);
                }
            }
        }
    }
}