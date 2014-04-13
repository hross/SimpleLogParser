using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public class StatefulParser
    {
        #region log4net
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        private MetricService _metricService;
        private PluginSettingsService _pluginSettingsService;
        private PluginDirectory _pluginDirectory;
        private List<IParserPlugin> _plugins;
        private SubscriberService _subscriberService;

        public Action<IParserPlugin, string, List<Subscriber>, Dictionary<string, string>> OnAlert { get; set; }

        public StatefulParser(
            MetricService metricService, 
            PluginSettingsService pluginSettingsService, 
            PluginDirectory pluginDirectory,
            SubscriberService subscriberService,
            Action<IParserPlugin, string, List<Subscriber>, Dictionary<string, string>> onAlert = null)
        {
            _metricService = metricService;
            _pluginSettingsService = pluginSettingsService;
            _pluginDirectory = pluginDirectory;
            _subscriberService = subscriberService;
            OnAlert = onAlert;

            if (null == OnAlert)
                OnAlert = (p, m, s, d) => { };
        }

        public void BatchStart()
        {
            InitializePlugins();
        }

        public void ParseLog(WatchedFile file)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            long lines = 0;
            string line;

            var thisFilePlugins = _plugins.Where(p => p.Enabled && p.BeforeFile(file.Path));

            log.InfoFormat("Starting file: {0}", file.Path);

            if (thisFilePlugins.Count() > 0)
            {

                using (var logFile = new LogFileReader(file.Path))
                {
                    if (file.LastReadFileSize > 0)
                        logFile.Seek(file.LastReadFileSize);

                    while ((line = logFile.ReadLine()) != null)
                    {
                        foreach (var plugin in thisFilePlugins)
                        {
                            plugin.ParseLine(file.Path, line);
                        }
                        lines++;
                    }

                    foreach (var plugin in thisFilePlugins)
                    {
                        plugin.AfterFile(file.Path);
                    }
                }
            }

            log.InfoFormat("{0} lines processed in {1}.", lines, watch.Elapsed);
        }

        public void BatchEnd()
        {
            foreach (var plugin in _plugins)
            {
                plugin.AfterBatch();

                var settings = _pluginSettingsService.SettingsFor(plugin.Name);
                settings.LastRunTimeUTC = DateTime.UtcNow;
                _pluginSettingsService.Update(settings);
            }
        }

        private void InitializePlugins()
        {
            _plugins = _pluginDirectory.AllPlugins();
            foreach (var plugin in _plugins)
            {
                var settings = _pluginSettingsService.SettingsFor(plugin.Name);
                plugin.Initialize(settings, _metricService);
                plugin.OnAlert = this.Alert;
            }
        }

        private void Alert(IParserPlugin plugin, string message, Dictionary<string, string> parameters)
        {
            var subscribers = plugin.Settings.Subscribers(this._subscriberService);
            this.OnAlert(plugin, message, subscribers, parameters);
        }
    }
}
