using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace SimpleLogParser.Common
{
    public class MetricService
    {
        private OrmLiteConnectionFactory _factory;

        public MetricService(string connectionString)
        {
            _factory = new OrmLiteConnectionFactory(connectionString, SqlServerDialect.Provider);
            _factory.Run(db => db.CreateTable<Metric>(overwrite: false));
            _factory.Run(db => db.CreateTable<Message>(overwrite: false));
            _factory.Run(db => db.CreateTable<Trend>(overwrite: false));
        }

        #region Metrics

        public void Increment(string metricName, int value = 1)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var metric = db.QuerySingle<Metric>(new { Name = metricName });

                if (null == metric)
                {
                    db.Insert<Metric>(new Metric { Name = metricName, Value = value });
                    return;
                }
                else
                {
                    metric.Value++;
                    db.Update<Metric>(metric);
                }
            }
        }

        public void Decrement(string metricName, int value = 0)
        {
            this.Increment(metricName, -1);
        }

        public int ValueOf(string metricName)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                var metric = db.QuerySingle<Metric>(new { Name = metricName });

                if (null == metric)
                    return 0;

                return metric.Value;
            }
        }

        #endregion
        
        #region Messages

        public void AddMessage(string category, string message)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                db.Insert<Message>(new Message { Category = category, Data = message });
            }
        }

        public List<Message> MessagesSince(string category, DateTime since = default(DateTime))
        {
            if (since == default(DateTime))
                since = SqlDateTime.MinValue.Value;

            using (IDbConnection db = _factory.OpenDbConnection())
            {
                if (!string.IsNullOrEmpty(category))
                    return db.Where<Message>(msg => msg.CreatedOnUTC > since && msg.Category == category);
                else
                    return db.Where<Message>(msg => msg.CreatedOnUTC > since);
            }
        }

        #endregion

        #region Trends

        private const int MaxTrends = 100;

        // keep last 100 trends in a category
        public void Trend(string name, int value = 1, DateTime? actionDateUtc = null)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                // add the latest
                var trend = new Trend { Name = name, Value = value };

                if (actionDateUtc.HasValue)
                    trend.ActionDateUTC = actionDateUtc.Value;

                db.Insert<Trend>(trend);

                // do we need to remove any?
                long count = db.Count<Trend>(t => t.Name == name);
                if (count > MaxTrends)
                {
                    long toDelete = count - MaxTrends;
                    db.ExecuteNonQuery(string.Format("DELETE FROM Trend WHERE Id IN (SELECT TOP {0} Id FROM Trend ORDER BY ActionDateUTC ASC)", toDelete));
                }
            }
        }

        public List<Trend> Trends(string name, int max = MaxTrends)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                return db.Select<Trend>(ev => ev.Where(t => t.Name == name).Limit(max).OrderByDescending(t => t.ActionDateUTC));
            }
        }

        /// <summary>
        /// Clear all trends of this name.
        /// </summary>
        /// <param name="name"></param>
        public void ClearTrends(string name)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                db.Delete<Trend>(t => t.Name == name);
            }
        }

        /// <summary>
        /// Clear all trends of this names since a certain date.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sinceDateUTC"></param>
        public void ClearTrendsSince(string name, DateTime sinceDateUTC)
        {
            using (IDbConnection db = _factory.OpenDbConnection())
            {
                db.Delete<Trend>(t => t.Name == name && t.ActionDateUTC < sinceDateUTC);
            }
        }

        #endregion
    }
}
