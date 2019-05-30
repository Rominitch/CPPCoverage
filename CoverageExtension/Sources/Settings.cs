using System;
using System.Xml;

namespace NubiloSoft.CoverageExt
{
    public class Settings
    {
        public static bool UseNativeCoverageSupport = true;

        /// <summary>
        /// Time of execution of subprocess coverage in ms.
        /// </summary>
        public static int timeoutCoverage = 60000;

        public static void ReadSettings(string solutionPath)
        {
            // Load settings if possible
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(solutionPath + "\\.coverage\\settings.xml");

                timeoutCoverage = Int32.Parse(doc.DocumentElement.SelectSingleNode("/timeoutCoverage").InnerText);
            }
            catch(Exception)
            {
                Console.WriteLine("Code coverage: Settings are not available.");
            }
        }

        public static void SaveSettings(string solutionPath)
        {
            string pathCover = solutionPath + "\\.coverage";
            try
            {
                // Build final directory if not exist
                if(System.IO.Directory.Exists(pathCover))
                {
                    System.IO.Directory.CreateDirectory(pathCover);
                }

                // Save settings
                XmlDocument doc = new XmlDocument();

                // Header
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = doc.DocumentElement;
                doc.InsertBefore(xmlDeclaration, root);

                XmlElement first = doc.CreateElement(string.Empty, "settings", string.Empty);
                doc.AppendChild(first);

                // TimeCoverage
                {
                    XmlElement timeCov = doc.CreateElement(string.Empty, "timeoutCoverage", string.Empty);
                    XmlText text2 = doc.CreateTextNode(timeoutCoverage.ToString());
                    timeCov.AppendChild(text2);
                    first.AppendChild(timeCov);
                }

                doc.Save(pathCover + "\\settings.xml");
            }
            catch (Exception)
            {
                Console.WriteLine("Code coverage: Impossible to create folder/settings.");
            }
        }
    }
}
