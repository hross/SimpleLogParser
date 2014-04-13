using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SimpleLogParser.Common
{
    public class PluginSettings
    {
        public PluginSettings()
        {
            this.LastRunTimeUTC = SqlDateTime.MinValue.Value;
            this.Settings = new Dictionary<string, string>();
            this.Enabled = true;
        }

        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public DateTime LastRunTimeUTC { get; set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public Dictionary<string, string> Settings { get; set; }

        public virtual List<Subscriber> Subscribers(SubscriberService subscriberService)
        {
            return subscriberService.Subscribers(this.Id);
        }
    }
}
