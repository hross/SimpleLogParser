using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public class LogUtility
    {
        /// <summary>
        /// NOTE: this will not return the date only the timestamp.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static DateTime FromTaskLogLine(string line)
        {
            var parts = line.Split(new string[] { " - " }, StringSplitOptions.None);

            try
            {
                parts[0] = parts[0].Replace(" AM", "").Replace(" PM", "");
                return DateTime.ParseExact(parts[0], "h:m:s", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime FromTaskLogLine(DateTime date, string line)
        {
            return CombineDateAndTime(date, FromTaskLogLine(line));
        }

        public static DateTime CombineDateAndTime(DateTime date, DateTime time)
        {
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }
    }
}
