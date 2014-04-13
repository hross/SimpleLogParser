using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogParser.Common
{
    public interface IParserPlugin
    {
        string Name { get; }

        PluginSettings Settings { get; }

        bool Enabled { get; set; }

        void Initialize(PluginSettings settings, MetricService metricService);

        void ParseLine(string filePath, string line);

        bool BeforeFile(string filePath);

        void AfterFile(string fileName);

        void AfterBatch();

        Action<IParserPlugin, string, Dictionary<string, string>> OnAlert { get; set; }
    }
}
