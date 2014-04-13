using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public class LogFileReader : IDisposable
    {
        #region File Properties

        protected readonly string FileName;
        private FileInfo _fileInfo;
        private FileStream _fileStream;
        private StreamReader _inputStream;

        protected StreamReader InputStream
        {
            get
            {
                return _inputStream ??
                       (_inputStream = new StreamReader(FileStream));
            }
        }

        private FileStream FileStream
        {
            get
            {
                return _fileStream ??
                       (_fileStream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            }
        }

        #endregion

        public LogFileReader(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("Could not open the specified file.", fileName);
            }

            FileName = fileName;
        }

        #region Basic File Methods

        public void Seek(long offset)
        {
            if (_inputStream != null)
            {
                _inputStream.Close();
                _inputStream = null;
            }

            FileStream.Seek(offset, SeekOrigin.Begin);
        }

        public string ReadLine()
        {
            return InputStream.ReadLine();
        }

        public long Offset
        {
            get { return FileStream.Position; }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(Boolean disposing)
        {
            if (_inputStream != null)
            {
                _inputStream.Close();
                _inputStream = null;
            }
        }
    }
}
