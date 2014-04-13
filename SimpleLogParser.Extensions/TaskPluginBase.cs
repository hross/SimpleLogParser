using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleLogParser.Common;

namespace SimpleLogParser.Extensions
{
    public abstract class TaskPluginBase : ParserPluginBase
    {
        public override void ParseLine(string filePath, string line)
        {
            base.ParseLine(filePath, line);

            string fileName = Path.GetFileName(filePath);

            var components = fileName.Split(new char[] { '-' });

            if (components.Length <= 1)
            {
                OnLine(fileName, DateTime.MinValue, string.Empty, line);
            }
            else
            {
                string[] data = fileName.Split(new char[] { '_' });
                DateTime dateTime = DateTime.MinValue;

                if (data.Length > 1)
                {
                    try { dateTime = DateTime.ParseExact(data[1].Replace(".log", ""), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture); }
                    catch { }
                }

                OnLine(data[0], dateTime, components[0], line);
            }
        }

        public abstract void OnLine(string taskName, DateTime dateTime, string className, string line);
    }
}
