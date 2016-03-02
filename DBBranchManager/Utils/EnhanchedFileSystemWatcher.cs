using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBBranchManager.Utils
{
    public delegate void EnhanchedFileSystemWatcherChangeEventHandler(object sender, EnhanchedFileSystemWatcherChangeEventArgs e);

    public class EnhanchedFileSystemWatcherChangeEventArgs : EventArgs
    {
        public EnhanchedFileSystemWatcherChangeEventArgs(string path, IEnumerable<object> affectedItems)
        {
            Path = path;
            AffectedItems = affectedItems.ToList();
        }

        public string Path { get; private set; }
        public IReadOnlyCollection<object> AffectedItems { get; private set; }
    }

    public class EnhanchedFileSystemWatcher : IDisposable
    {
        private readonly Dictionary<string, FileSystemWatcher> mWatchersByDir;
        private readonly Dictionary<string, List<ItemInfo>> mItemsByDir;
        private bool mDisposed;

        private class ItemInfo
        {
            public object Item { get; private set; }
            public Regex Regex { get; private set; }

            public ItemInfo(object item, Regex regex)
            {
                Item = item;
                Regex = regex;
            }
        }

        public EnhanchedFileSystemWatcher()
        {
            mWatchersByDir = new Dictionary<string, FileSystemWatcher>(StringComparer.InvariantCultureIgnoreCase);
            mItemsByDir = new Dictionary<string, List<ItemInfo>>(StringComparer.InvariantCultureIgnoreCase);
        }

        public event EnhanchedFileSystemWatcherChangeEventHandler Changed;

        public void AddWatch(string path, object item)
        {
            var p = GetPathData(path);
            string filter;

            if (p.IsFilePath)
                filter = string.Format("^{0}$", Regex.Escape(p.FileName));
            else
                filter = "^.*$";

            AddWatchImpl(p.DirName, filter, item);
        }

        public void AddWatch(string path, string filter, object item)
        {
            var p = GetPathData(path);
            var dirName = p.IsFilePath ?
                Path.GetFullPath(string.Format("{0}{1}", path, Path.DirectorySeparatorChar)) :
                p.DirName;

            AddWatchImpl(dirName, filter, item);
        }

        private void AddWatchImpl(string directory, string filter, object item)
        {
            EnsureWatcher(directory);
            var items = GetItems(directory);

            items.Add(new ItemInfo(item, new Regex(filter, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)));
        }

        private List<ItemInfo> GetItems(string directory)
        {
            List<ItemInfo> items;
            if (!mItemsByDir.TryGetValue(directory, out items))
            {
                mItemsByDir[directory] = items = new List<ItemInfo>();
            }

            return items;
        }

        private FileSystemWatcher EnsureWatcher(string directory)
        {
            FileSystemWatcher watcher;
            if (!mWatchersByDir.TryGetValue(directory, out watcher))
            {
                var dirToWatch = GetDeepestExistingDirectory(directory);
                if (!StringComparer.InvariantCultureIgnoreCase.Equals(dirToWatch, directory))
                    return EnsureWatcher(dirToWatch);

                // If we have a watcher for a lower-level directory, use it
                var lowerLevelWatcher = mWatchersByDir.SingleOrDefault(x => directory.StartsWith(x.Key, StringComparison.InvariantCultureIgnoreCase));
                if (lowerLevelWatcher.Key != null)
                {
                    return lowerLevelWatcher.Value;
                }

                // If we have a watcher for a higher-level directory, lower its level and use it
                var higherLevelWatcher = mWatchersByDir.FirstOrDefault(x => x.Key.StartsWith(directory, StringComparison.InvariantCultureIgnoreCase));
                if (higherLevelWatcher.Key != null)
                {
                    higherLevelWatcher.Value.Path = directory;
                    mWatchersByDir.Remove(higherLevelWatcher.Key);
                    mWatchersByDir.Add(directory, higherLevelWatcher.Value);
                    return higherLevelWatcher.Value;
                }

                // We don't have a suitable watcher. Create one.
                watcher = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite
                };
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;

                mWatchersByDir.Add(directory, watcher);
                return watcher;
            }

            return watcher;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            OnChangedInternal(e.FullPath);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            OnChangedInternal(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            OnChangedInternal(e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            OnChangedInternal(e.OldFullPath);
            OnChangedInternal(e.FullPath);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Error in watcher for '{0}':\n{1}", ((FileSystemWatcher)sender).Path, e.GetException());
        }

        private void OnChangedInternal(string fullPath)
        {
            var evt = Changed;
            if (evt != null)
            {
                var items = mItemsByDir
                    .Where(x => fullPath.StartsWith(x.Key, StringComparison.InvariantCultureIgnoreCase))
                    .SelectMany(x => x.Value.Select(y => new { PrefixLen = x.Key.Length, Item = y }))
                    .Where(x => x.Item.Regex.IsMatch(fullPath.Substring(x.PrefixLen)))
                    .Select(x => x.Item.Item)
                    .ToList();
                if (items.Any())
                {
                    evt(this, new EnhanchedFileSystemWatcherChangeEventArgs(fullPath, items));
                }
            }
        }

        private static string GetDeepestExistingDirectory(string dir)
        {
            while (true)
            {
                if (string.IsNullOrEmpty(dir))
                    throw new ArgumentException("dir");
                if (Directory.Exists(dir))
                    return Path.GetFullPath(string.Format("{0}{1}", dir, Path.DirectorySeparatorChar));

                dir = Path.GetDirectoryName(dir);
            }
        }

        private struct PathData
        {
            private readonly string mDirName;
            private readonly string mFileName;
            private readonly bool mIsFilePath;

            public PathData(string dirName, string fileName, bool isFilePath)
            {
                mDirName = dirName;
                mFileName = fileName;
                mIsFilePath = isFilePath;
            }

            public string DirName
            {
                get { return mDirName; }
            }

            public string FileName
            {
                get { return mFileName; }
            }

            public bool IsFilePath
            {
                get { return mIsFilePath; }
            }
        }

        private static PathData GetPathData(string path)
        {
            path = Path.GetFullPath(path);
            var fileName = Path.GetFileName(path);

            if (fileName == string.Empty)
            {
                return new PathData(path, string.Empty, false);
            }

            return new PathData(string.Format("{0}{1}", Path.GetDirectoryName(path), Path.DirectorySeparatorChar), fileName, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed)
                return;

            if (disposing)
            {
                foreach (var watcher in mWatchersByDir.Values)
                {
                    watcher.Changed -= OnChanged;
                    watcher.Created -= OnCreated;
                    watcher.Deleted -= OnDeleted;
                    watcher.Error -= OnError;
                    watcher.Dispose();
                }
                mWatchersByDir.Clear();
                mItemsByDir.Clear();
            }

            mDisposed = true;
        }
    }
}