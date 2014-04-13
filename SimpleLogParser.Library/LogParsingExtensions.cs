using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public static class LogParsingExtensions
    {
        public static T? HasAfter<T>(Func<string, T> parse, string line, string beforeText, string afterText = " ")
            where T : struct, IComparable
        {
            var pos = line.IndexOf(beforeText);

            if (pos <= 0)
                return null;

            var afterPos = line.IndexOf(afterText, pos + beforeText.Length);
            string val;

            if (afterPos <= 0)
            {
                val = line.Substring(pos + beforeText.Length, line.Length - (pos + beforeText.Length));
            }
            else
            {
                val = line.Substring(pos + beforeText.Length, afterPos - (pos + beforeText.Length));
            }

            try
            {
                return parse(val);
            }
            catch
            {
                return null;
            }
        }

        public static T? HasBefore<T>(Func<string, T> parse, string line, string afterText, string beforeText = " ")
            where T : struct, IComparable
        {
            var pos = line.IndexOf(afterText);

            if (pos <= 0)
                return null;

            var beforePos = line.LastIndexOf(beforeText, pos-1);
            string val;

            if (beforePos <= 0)
            {
                val = line.Substring(0, pos);
            }
            else
            {
                val = line.Substring(beforePos, pos - beforePos);
            }

            try
            {
                return parse(val);
            }
            catch
            {
                return null;
            }
        }

        public static double? HasDoubleAfter(this string line, string beforeText, string afterText = " ")
        {
            return HasAfter<double>(double.Parse, line, beforeText, afterText);
        }

        public static double? HasDoubleBefore(this string line, string afterText, string beforeText = " ")
        {
            return HasBefore<double>(double.Parse, line, afterText, beforeText);
        }

        public static int? HasIntAfter(this string line, string beforeText, string afterText = " ")
        {
            return HasAfter<int>(int.Parse, line, beforeText, afterText);
        }

        public static int? HasIntBefore(this string line, string afterText, string beforeText = " ")
        {
            return HasBefore<int>(int.Parse, line, afterText, beforeText);
        }

        public static DateTime? HasDateTimeAfter(this string line, string beforeText, string afterText = " ")
        {
            return HasAfter<DateTime>(DateTime.Parse, line, beforeText, afterText);
        }

        public static DateTime? HasDateTimeBefore(this string line, string afterText, string beforeText = " ")
        {
            return HasBefore<DateTime>(DateTime.Parse, line, afterText, beforeText);
        }
    }
}
