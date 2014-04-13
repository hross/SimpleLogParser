using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SimpleLogParser.Common;

namespace SimpleLogParser
{
    public class SimpleLogParserService
    {
        #region log4net
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        public const int MaxFilesPerBatch = 500;
        public const int MaxParallelFiles = 10;

        // run every 30 minutes
        public const double FrequencyIntervalMilliseconds = 1000 * 60;

        #region Timer and Sync Stuff

        readonly Timer _timer;
        public void Start() { _timer.Start(); }
        public void Stop() { _timer.Stop(); }

        private bool _running = false;
        private object _sync = new object();
        private void SynchronizedMain()
        {

            lock (_sync)
            {
                if (_running) return;
                _running = true;
            }

            try
            {
                DoIt();
            }
            finally
            {
                lock (_sync)
                {
                    _running = false;
                }
            }
        }

        #endregion

        /// <summary>
        /// Public constructor for service. Here is where we start our file watcher and launch
        /// our periodic logging.
        /// </summary>
        public SimpleLogParserService(bool asMonitor = false)
        {
            _asMonitor = asMonitor;

            string connectionString = ConfigurationManager.ConnectionStrings["SimpleLogParserDataSource"].ConnectionString;
            // set up our log parser
            var metricService = new MetricService(connectionString);
            var settingsService = new PluginSettingsService(connectionString);
            var subscriberService = new SubscriberService(connectionString);
            var directory = new PluginDirectory();

            _service = new StatefulParser(metricService, settingsService, directory, subscriberService, OnAlert);

            string path = ConfigurationManager.AppSettings["WatchDirectory"];
            bool includeSubdirectories = false;

            try { includeSubdirectories = bool.Parse(ConfigurationManager.AppSettings["IncludeSubdirectories"]); }
            catch { }

            _queue = new FileSystemQueue(path, connectionString, includeSubdirectories);

            int intervalMinutes = int.Parse(ConfigurationManager.AppSettings["IntervalMinutes"] ?? "30");

            // if we want active monitoring we need to start the watcher
            if (asMonitor)
                _queue.Start();

            _timer = new Timer(FrequencyIntervalMilliseconds * intervalMinutes) { AutoReset = true };
            _timer.Elapsed += (sender, eventArgs) => this.SynchronizedMain();

            this.SynchronizedMain(); // call the timer callback right away to start
        }

        private bool _asMonitor;
        private StatefulParser _service;
        private FileSystemQueue _queue;
        private int _errorCount = 0;

        /// <summary>
        /// The main method. It will only run if it is not already running.
        /// </summary>
        private void DoIt()
        {
            try
            {
                // if we aren't running in realtime we need to update our
                // existing file queue on each run
                if (!_asMonitor)
                    _queue.Initialize();

                List<WatchedFile> files;
                bool processAll = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ProcessAll"]);
                do
                {
                    files = _queue.UnprocessedFiles(MaxFilesPerBatch);

                    if (files.Count > 0)
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        log.Debug("Batch started.");
                        _service.BatchStart();

                        // process up to 5 files at a time
                        Parallel.ForEach(files,
                            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelFiles },
                            file =>
                            {
                                if (_queue.StartProcessing(file))
                                {
                                    // we successfully got a lock to process this file
                                    try
                                    {
                                        _service.ParseLog(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(string.Format("Problem parsing file: {0}", file.Path), ex);
                                    
                                          string adminEmail = ConfigurationManager.AppSettings["FromEmail"];

                                          if (!string.IsNullOrEmpty(adminEmail))
                                          {
                                              // error alert with dummy plugin
                                              OnAlert(new AdminPlugin(adminEmail), string.Format("Error processing batch: {0}", ex.Message), null, null);
                                          }
                                    }
                                    finally
                                    {
                                        // we finished processing this file
                                        _queue.StopProcessing(file);
                                    }
                                }
                            });

                        _service.BatchEnd();
                        sw.Stop();
                        log.Debug(string.Format("Batch ended. {0} files processed in {1}", files.Count, sw.Elapsed));
                    }

                    // only do one batch if we aren't processing everything (i.e. just teting)
                } while (processAll && files.Count == MaxFilesPerBatch);
            }
            catch (Exception ex)
            {
                log.Error("Unable to process batch!", ex);
                _errorCount++;

                string adminEmail = ConfigurationManager.AppSettings["FromEmail"];

                if (!string.IsNullOrEmpty(adminEmail))
                {
                    // error alert with dummy plugin
                    OnAlert(new AdminPlugin(adminEmail), string.Format("Error processing batch: {0}", ex.Message), null, null);
                }

                if (_errorCount > 10)
                    this.Stop();
            }
        }


        #region Basic Alerting Methods

        public static void OnAlert(IParserPlugin plugin, string message, List<Subscriber> subscribers, Dictionary<string, string> parameters)
        {
            string smtpServer = ConfigurationManager.AppSettings["SMTPServer"];
            string fromEmail = ConfigurationManager.AppSettings["FromEmail"] ?? "logalert@logalert.com";
            string subscriberSuffix = ConfigurationManager.AppSettings["SubscriberSuffix"];
            string bccAddress = ConfigurationManager.AppSettings["NotificationBCCAddress"];

            List<string> to;

            if (null == subscribers || subscribers.Count == 0)
            {
                if (!string.IsNullOrEmpty(bccAddress))
                {
                    to = new List<string> { bccAddress };
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(subscriberSuffix))
                {
                    to = subscribers.ConvertAll(s => s.Name);
                }
                else
                {
                    to = subscribers.ConvertAll(s => s.Name + subscriberSuffix);
                }
            }

            SendEmail(smtpServer, fromEmail, to, string.Format("LogAlert From {0}", plugin.Name), message, bccAddress);
        }


        private static void SendEmail(string smtpServer, string from, List<string> to, string subject, string body, string bccAddress = "")
        {
            MailMessage notification = new MailMessage();
            foreach (string toaddr in to)
            {
                notification.To.Add(toaddr);
            }
            notification.Subject = subject;
            notification.From = new MailAddress(from);
            notification.Body = body;

            if (!string.IsNullOrEmpty(bccAddress))
                notification.Bcc.Add(bccAddress);

            SmtpClient smtp = new SmtpClient(smtpServer);
            smtp.Send(notification);
        }

        #endregion

        #region Admin Alert Classes
        
        /// <summary>
        /// Dummy admin plugin class for sending admin error emails.
        /// </summary>
        private class AdminPlugin : ParserPluginBase
        {
            private string _email;

            public AdminPlugin(string email)
            {
                _email = email;
            }

            public override PluginSettings Settings
            {
                get
                {
                    return new AdminSettings(_email);

                }
            }
        }

        private class AdminSettings : PluginSettings
        {
            private string _email;

            public AdminSettings(string email) { _email = email; }

            public override List<Subscriber> Subscribers(SubscriberService subscriberService)
            {
                return new List<Subscriber> { new Subscriber { Name = _email } };
            }
        }

        #endregion
    }
}
