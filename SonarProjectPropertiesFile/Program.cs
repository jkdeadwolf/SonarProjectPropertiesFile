using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SonarProjectPropertiesFile
{
    class Program
    {
        private const string DEFAULT_VERSION = "M";

        static void Main(string[] args)
        {
            string propertiesFileName = args[0] + @"\sonar-project.properties";

            if (File.Exists(propertiesFileName) == true)
            {
                File.Copy(propertiesFileName, propertiesFileName + ".bak", true);
            }

            DirectoryInfo di = new DirectoryInfo(args[0]);
            string projectId = di.Name;

            string projectName = projectId;
            Console.WriteLine(string.Format("args.Length {0}", args.Length));
            if (args.Length == 2)
            {
                projectName = args[1];
            }

            string versionId = DEFAULT_VERSION;
            string parentName = di.Parent.Name;
            if (Regex.IsMatch(parentName, @"V\d+", RegexOptions.IgnoreCase) == true)
            {
                versionId = parentName;
            }
            //projectName = string.Format("{0}-{1}", projectName, versionId);
            string projectKey = string.Format("{0}-{1}", projectId, versionId);

            string classpathFile = args[0] + @"\.classpath";
            string classpathFileText = string.Empty;
            try
            {
                classpathFileText = File.ReadAllText(classpathFile);
            }
            catch (FileNotFoundException ex)
            {
                return;
            }

            MatchCollection srcMatchs = Regex.Matches(classpathFileText, @"classpathentry .*kind=""src"" path=""(?<SRC>.*)""", RegexOptions.IgnoreCase);
            StringBuilder srcModules = new StringBuilder();

            string modules = string.Empty;

            foreach (Match match in srcMatchs)
            {
                srcModules.AppendLine(string.Format(@"{0}.sonar.sources={0}", match.Groups["SRC"].Value));
                srcModules.AppendLine(string.Format(@"{0}.sonar.projectBaseDir=.", match.Groups["SRC"].Value));
                srcModules.AppendLine("");

                modules = string.Format("{0}{1},", modules, match.Groups["SRC"].Value);
                
            }


            string contentPatten = @"sonar.projectKey={0}
sonar.projectName={1}
sonar.projectVersion={2}

# Modules inherit properties set at parent level
# sonar.sources=src
sonar.sourceEncoding=UTF-8
sonar.language=java

# Set modules IDs
sonar.modules={3}


# Default module base directory is <curent_directory>/<module_ID>
# It has to be overriden for module2
{4}";
            string contentText = string.Format(contentPatten, projectKey, projectName, versionId, modules, srcModules.ToString());
            File.WriteAllText(propertiesFileName, contentText, Encoding.UTF8);
        }
    }
}
