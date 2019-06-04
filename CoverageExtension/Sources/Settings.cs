using System;
using System.Collections;
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

        public static string solutionPath;
        public static ArrayList exclude;

        public static void ReadSettings()
        {
            if (String.IsNullOrEmpty(solutionPath))
            {
                throw new InvalidOperationException("Missing to initialize solution");
            }

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

        public static void SaveSettings()
        {
            if (String.IsNullOrEmpty(solutionPath))
            {
                throw new InvalidOperationException("Missing to initialize solution");
            }

            string pathCover = solutionPath + "\\.coverage";
            try
            {
                // Build final directory if not exist
                if(!System.IO.Directory.Exists(pathCover))
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

                // Exclude
                if(Settings.exclude != null)
                {
                    XmlElement excludes = doc.CreateElement(string.Empty, "Excludes", string.Empty);

                    foreach(string ex in exclude)
                    {
                        XmlElement exclude = doc.CreateElement(string.Empty, "Exclude", string.Empty);
                        XmlText text = doc.CreateTextNode(ex);
                        exclude.AppendChild(text);
                        excludes.AppendChild(exclude);
                    }
                    
                    first.AppendChild(excludes);
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
