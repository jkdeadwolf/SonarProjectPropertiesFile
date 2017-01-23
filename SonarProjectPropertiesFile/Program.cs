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

        static bool EclipseProject(string rootPath, out string modules, out string srcModules)
        {
            string classpathFile = rootPath + @"\.classpath";
            string classpathFileText = string.Empty;

            modules = string.Empty;
            srcModules = string.Empty;

            try
            {
                classpathFileText = File.ReadAllText(classpathFile);
            }
            catch (FileNotFoundException ex)
            {
                return false;
            }

            MatchCollection srcMatchs = Regex.Matches(classpathFileText, @"classpathentry .*kind=""src"" path=""(?<SRC>.*)""", RegexOptions.IgnoreCase);
            StringBuilder sbModules = new StringBuilder();

            modules = string.Empty;

            foreach (Match match in srcMatchs)
            {
                sbModules.AppendLine(string.Format(@"{0}.sonar.sources={0}", match.Groups["SRC"].Value));
                sbModules.AppendLine(string.Format(@"{0}.sonar.projectBaseDir=.", match.Groups["SRC"].Value));
                sbModules.AppendLine("");

                modules = string.Format("{0}{1},", modules, match.Groups["SRC"].Value);

            }

            srcModules = sbModules.ToString();
            return true;
        }

        static bool GradleProject(string rootPath, out string modules, out string srcModules)
        {
            modules = string.Empty;
            srcModules = string.Empty;

            string settingsFile = rootPath + @"\settings.gradle";
            string settingsFileText = string.Empty;

            List<string> projects = new List<string>();

            try
            {
                settingsFileText = File.ReadAllText(settingsFile);
                MatchCollection mcInclude = Regex.Matches(settingsFileText, "^include (?<Projects>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in mcInclude)
                {
                    string projectsText = match.Groups["Projects"].Value;
                    string[] projectArray = projectsText.Split(',');
                    foreach (string prj in projectArray)
                    {
                        projects.Add(prj.Trim(@"' ".ToCharArray()).Replace(":", ""));
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                projects.Add("");
            }

            StringBuilder sbModules = new StringBuilder();
            foreach (string projectPath in projects)
            {
                if (File.Exists(string.Format(@"{0}\{1}\build.gradle",rootPath, projectPath)) == true)
                {
                    sbModules.AppendLine(string.Format(@"{0}.sonar.sources=src", projectPath));
                    sbModules.AppendLine(string.Format(@"{0}.sonar.projectBaseDir={0}\", projectPath));
                    sbModules.AppendLine("");

                    modules = string.Format(@"{0}{1},", modules, projectPath);
                }
            }

            srcModules = sbModules.ToString();
            return true;
        }

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
            string modules, srcModules;

            if (EclipseProject(args[0], out modules, out srcModules) == false)
            {
                GradleProject(args[0], out modules, out srcModules);
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
            string contentText = string.Format(contentPatten, projectKey, projectName, versionId, modules, srcModules);
            File.WriteAllText(propertiesFileName, contentText, Encoding.UTF8);
        }
    }
}
