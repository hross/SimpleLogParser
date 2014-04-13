using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using System.Linq;

namespace SimpleLogParser.Common
{
    public class PluginSettingsService
    {
        private OrmLiteConnectionFactory _factory;

        public PluginSettingsService(string connectionString)
        {
            _factory = new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            _factory.Run(db => db.CreateTable<PluginSettings>(overwrite: false));
        }

        public List<PluginSettings> AllSettings()
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                return db.Select<PluginSettings>();
            }
        }

        public PluginSettings SettingsFor(string pluginName, bool autoCreate = true)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var settings = db.Select<PluginSettings>(ps => ps.Name == pluginName).FirstOrDefault();

                if (null == settings && autoCreate)
                {
                    settings = new PluginSettings { Name = pluginName };
                    db.Insert<PluginSettings>(settings);
                }

                return settings;
            }
        }

        public void Update(PluginSettings settings)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                db.Update<PluginSettings>(settings);
            }
        }
    }
}
