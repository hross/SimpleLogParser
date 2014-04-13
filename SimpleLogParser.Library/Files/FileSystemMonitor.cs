using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace SimpleLogParser.Common
{
    internal class FileSystemMonitor : IDisposable
    {
        private FileSystemWatcher _watcher = null;
        private OrmLiteConnectionFactory _factory;

        public FileSystemMonitor(string baseDirectory, string connectionString)
        {
            _factory = new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            _factory.Run(db => db.CreateTable<WatchedFile>(overwrite: false));

            _watcher = new FileSystemWatcher();
            _watcher.Path = baseDirectory;
            _watcher.IncludeSubdirectories = true;

            // we only care about file changes, not directories and we only care about size changes and writes
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;

            //// Only watch text files.
            //_watcher.Filter = "*.txt";

            _watcher.Changed += new FileSystemEventHandler(OnChanged);
            _watcher.Created += new FileSystemEventHandler(OnCreated);
            _watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            _watcher.Renamed += new RenamedEventHandler(OnRenamed);

            _watcher.EnableRaisingEvents = true;
        }


        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var info = new FileInfo(e.FullPath);
                var file = new WatchedFile { Path = e.FullPath, LastReadFileSize = 0, CurrentFileSize = info.Length };
                db.Insert<WatchedFile>(file);
            }
        }
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                db.Delete<WatchedFile>(f => f.Path == e.FullPath);
            }
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var file = db.QuerySingle<WatchedFile>(new { Path = e.OldFullPath });
                db.UpdateOnly<WatchedFile>(file, ev => ev.Update(f => f.Path).Where(f => f.Path == e.OldFullPath));
            }
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var info = new FileInfo(e.FullPath);
                WatchedFile file = new WatchedFile { CurrentFileSize = info.Length };
                db.UpdateOnly<WatchedFile>(file, ev => ev.Update(f => f.CurrentFileSize).Where(f => f.Path == e.FullPath));
            }
        }

        public void Dispose()
        {
            if (null != _watcher)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }
    }
}
