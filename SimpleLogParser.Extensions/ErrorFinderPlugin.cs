using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleLogParser.Common;

namespace SimpleLogParser.Extensions
{
    /// <summary>
    /// Parse any VMStrackingWebLog files, check for deltas of over 10 minutes.
    /// 
    /// If we find any series of 3 deltas over 10 minutes, alert.
    /// </summary>
    [Export(typeof(IParserPlugin))]
    public class ErrorFinderPlugin : TaskPluginBase
    {
        #region log4net
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        public override void OnLine(string taskName, DateTime dateTime, string className, string line)
        {
            if (line.Contains("ERROR") || line.Contains("Error"))
            {
                this.Alert(string.Format("We found a potential error:\n\n{0}", line));
            }
        }
    }
}
