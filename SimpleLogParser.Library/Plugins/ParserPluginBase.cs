using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public abstract class ParserPluginBase : IParserPlugin
    {
        public ParserType ParserType { get; protected set; }

        /// <summary>
        /// Unique name of the plugin. By default it is the type name.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Settings to initialize the plugin.
        /// </summary>
        public virtual PluginSettings Settings { get; private set; }

        protected MetricService MetricService { get; private set; }

        public bool Enabled { get { return this.Settings.Enabled; } set { this.Settings.Enabled = value; } }

        public Action<IParserPlugin, string, Dictionary<string, string>> OnAlert { get; set; }
        
        /// <summary>
        /// This must be an empty constructor to handle MEF instantiation.
        /// </summary>
        public ParserPluginBase()
        {
            this.Name = this.GetType().FullName;
            this.Settings = new PluginSettings();
            this.ParserType = ParserType.Simple;
            this.OnAlert = (plugin, msg, dictionary) => { };
        }

        /// <summary>
        /// Initialize the plugin with settings from the database. This is called once.
        /// </summary>
        /// <param name="settings"></param>
        public virtual void Initialize(PluginSettings settings, MetricService metricService)
        {
            this.Settings = settings;
            this.MetricService = metricService;
        }

        /// <summary>
        /// Parse a log file line. This is called on every line.
        /// </summary>
        public virtual void ParseLine(string filePath, string line)
        {
            // do nothing right now
        }

        /// <summary>
        /// Inidicates we are moving to the next file. Returns whether or not we should parse this file
        /// with this plugin.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public virtual bool BeforeFile(string filePath)
        {
            return true;
        }

        /// <summary>
        /// Executes after a file is completed.
        /// </summary>
        /// <param name="fileName"></param>
        public virtual void AfterFile(string fileName)
        {
        }

        /// <summary>
        /// Execute after a batch of files is completed.
        /// </summary>
        public virtual void AfterBatch()
        {
        }

        /// <summary>
        /// Fire an alert from the plugin based on some condition.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        protected void Alert(string message, Dictionary<string,string> parameters = null)
        {
            this.OnAlert(this, message, parameters);
        }
    }
}
