using System.IO;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    ///<summary>
    ///A set of method invoked by Unity Cloud Build during the build process
    ///</summary>
    public static class CloudBuildHelpers
    {
        const string k_AdditionalBuildFilesFolder = "AdditionalBuildFiles/";

        /// <summary>
        /// Method called from CloudBuild when the build finishes.
        /// Needs to be referenced in the settings in CloudBuild's dashboard
        /// </summary>
        /// <param name="exportPath">The path where the build is</param>
        public static void PostExport(string exportPath)
        {
            FileAttributes attr = File.GetAttributes(exportPath);
            string directory;
            if (attr.HasFlag(FileAttributes.Directory))
            {
                directory = exportPath;
            }
            else
            {
                directory = Path.GetDirectoryName(exportPath);
            }
            CopyDirectory(k_AdditionalBuildFilesFolder, directory, true);
        }

        static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destDirName, file.Name), false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    CopyDirectory(subdir.FullName, Path.Combine(destDirName, subdir.Name), copySubDirs);
                }
            }
        }
    }
}
