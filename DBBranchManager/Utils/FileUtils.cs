using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBBranchManager.Utils
{
    internal static class FileUtils
    {
        public static IEnumerable<string> EnumerateFiles(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(Path.GetFileName)
                .Where(filter)
                .OrderBy(x => x, new NaturalSortComparer());
        }

        public class FileData
        {
            private readonly string mFullPath;
            private readonly string mFileName;

            public FileData(string fullPath)
            {
                mFullPath = fullPath;
                mFileName = Path.GetFileName(fullPath);
            }

            public string FullPath
            {
                get { return mFullPath; }
            }

            public string FileName
            {
                get { return mFileName; }
            }
        }

        public static IEnumerable<FileData> EnumerateFiles2(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(x => new FileData(x))
                .Where(x => filter(x.FileName))
                .OrderBy(x => x.FileName, new NaturalSortComparer());
        }

        public static void DeleteDirectory(string directory)
        {
            var dir = new DirectoryInfo(directory);
            RemoveReadOnlyRecursive(dir);
            dir.Delete(true);
        }

        private static void RemoveReadOnlyRecursive(DirectoryInfo dir)
        {
            foreach (var subDirectory in dir.GetDirectories())
            {
                RemoveReadOnlyRecursive(subDirectory);
            }
            foreach (var fileInfo in dir.GetFiles())
            {
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }
}