using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SimpleLogParser.Common
{
    public class Trend
    {
        public Trend()
        {
            this.ActionDateUTC = DateTime.UtcNow;
        }

        [Index]
        public string Name { get; set; }

        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [Index]
        public DateTime ActionDateUTC { get; set; }

        public int Value { get; set; }
    }
}
