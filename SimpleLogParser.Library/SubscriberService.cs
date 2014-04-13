using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace SimpleLogParser.Common
{
    public class SubscriberService
    {
        private OrmLiteConnectionFactory _factory;

        public SubscriberService(string connectionString)
        {
            _factory = new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            _factory.Run(db => db.CreateTable<Subscriber>(overwrite: false));
        }

        public List<Subscriber> Subscribers(int pluginSettingId)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                return db.Where<Subscriber>(s => s.PluginSettingId == pluginSettingId);
            }
        }
    }
}
