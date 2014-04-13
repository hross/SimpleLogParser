using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SimpleLogParser.Common
{
    /// <summary>
    /// TODO: add to queue table of files
    /// 
    /// if it changes update last read file size so we can reread a file
    /// </summary>
    public class WatchedFile
    {
        public WatchedFile()
        {
            this.ProcessStartTimeUTC = null;
            this.Processing = false;
            this.CurrentFileSize = 0;
            this.LastReadFileSize = 0;
            this.Path = string.Empty;
        }

        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public DateTime? ProcessStartTimeUTC { get; set; }

        [Index]
        public bool Processing { get; set; }

        [Index]
        public string Path { get; set; }

        [Index]
        public long LastReadFileSize { get; set; }

        [Index]
        public long CurrentFileSize { get; set; }
    }
}
