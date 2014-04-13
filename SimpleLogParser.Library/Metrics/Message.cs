using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SimpleLogParser.Common
{
    public class Message
    {
        public Message()
        {
            this.CreatedOnUTC = DateTime.UtcNow;
        }

        [AutoIncrement]
        public int Id { get; set; }

        public string Category { get; set; }

        public string Data { get; set; }

        public DateTime CreatedOnUTC { get; set; }
    }
}
