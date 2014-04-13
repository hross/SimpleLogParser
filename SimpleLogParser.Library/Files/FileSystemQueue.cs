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
    public sealed class FileSystemQueue : IDisposable
    {
        public const int MaxFileThreadsAtOneTime = 15;

        private FileSystemMonitor _monitor;
        private OrmLiteConnectionFactory _factory;

        private string _baseDirectory;
        private string _connectionString;
        private bool _includeSubdirectories;

        public FileSystemQueue(string baseDirectory, string connectionString, bool includSubdirectories = true)
        {
            _baseDirectory = baseDirectory;
            _connectionString = connectionString;

            _factory = new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            _factory.Run(db => db.CreateTable<WatchedFile>(overwrite: false));
        }

        #region File System Sync/Watch

        /// <summary>
        /// Create or check an initial index of files since last time we started
        /// </summary>
        private void Initialize(string baseDirectory, bool includeSubdirectories = true)
        {
            DirectoryInfo directory = new DirectoryInfo(baseDirectory);

            //foreach (FileInfo file in directory.GetFiles())
            //{
            var files = directory.GetFiles();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = MaxFileThreadsAtOneTime }, file =>
            {
                using (IDbConnection db = _factory.OpenDbConnection())
                {
                    var wf = db.QuerySingle<WatchedFile>(new { Path = file.FullName });

                    if (null == wf)
                    {
                        // create the file since it didn't exist
                        wf = new WatchedFile { Path = file.FullName, LastReadFileSize = 0, CurrentFileSize = file.Length };
                        db.Insert<WatchedFile>(wf);
                    }
                    else if (file.Length != wf.CurrentFileSize)
                    {
                        // update file size if necessary
                        wf.CurrentFileSize = file.Length;
                        db.UpdateOnly<WatchedFile>(wf, ev => ev.Update(f => f.CurrentFileSize).Where(f => f.Path == file.FullName));
                    }
                }
            });

            if (includeSubdirectories)
            {
                foreach (DirectoryInfo di in directory.GetDirectories())
                {
                    this.Initialize(di.FullName, includeSubdirectories);
                }
            }
        }

        public void Initialize()
        {
            List<WatchedFile> files;
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                files = db.Where<WatchedFile>(wf => wf.Path.StartsWith(_baseDirectory));
            }

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = MaxFileThreadsAtOneTime }, file =>
                {
                    using (IDbConnection db = _factory.OpenDbConnection())
                    {
                        // remove any files from the DB that no longer exist
                        if (!File.Exists(file.Path))
                            db.Delete<WatchedFile>(file);
                    }
                });


            // sync all files and sizes left over
            this.Initialize(_baseDirectory, _includeSubdirectories);
        }

        public void Start()
        {
            if (null == _monitor)
            {
                this.Initialize();

                _monitor = new FileSystemMonitor(_baseDirectory, _connectionString);
            }
        }

        public void Stop()
        {
            if (null != _monitor)
            {
                _monitor.Dispose();
                _monitor = null;
            }
        }

        public void Reset()
        {
            this.Stop(); // make sure we are stopped

            using (IDbConnection db = _factory.OpenDbConnection())
            {
                // remove anything in this directory
                db.Delete<WatchedFile>(wf => wf.Path.StartsWith(_baseDirectory));
            }

            // re-init all the files in this directory
            this.Initialize(_baseDirectory);
        }

        #endregion

        #region File Processing

        public List<WatchedFile> UnprocessedFiles(int max = 10)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                return db.Select<WatchedFile>(ev => ev.Where(wf => !wf.Processing && wf.LastReadFileSize != wf.CurrentFileSize && wf.CurrentFileSize > 0).Limit(max));
            }
        }

        /// <summary>
        /// Try to pick a file off the queue. It might be that something else already started working.
        /// If so, return false so we can ignore it.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool StartProcessing(WatchedFile file)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                file.Processing = true;
                return
                    db.UpdateOnly<WatchedFile>(file, ev => ev.Update(f => f.Processing).Where(f => f.Id == file.Id && !f.Processing)) > 0;
            }
        }

        public void StopProcessing(WatchedFile file)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                file.Processing = false;
                file.LastReadFileSize = file.CurrentFileSize;
                db.UpdateOnly<WatchedFile>(file, ev => ev.Update(new List<string> { "Processing", "LastReadFileSize" }).Where(f => f.Id == file.Id));
            }
        }
       
        #endregion

        public void Dispose()
        {
            Stop();
        }
    }
}
