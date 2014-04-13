using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SimpleLogParser.Common
{
    public class Subscriber
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [Index]
        public int PluginSettingId { get; set; }

        public string Name { get; set; }
    }
}
