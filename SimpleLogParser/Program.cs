using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Topshelf;
using SimpleLogParser.Common;

namespace SimpleLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<SimpleLogParserService>(s =>
                {
                    s.ConstructUsing(name => new SimpleLogParserService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Simple Log Parser");
                x.SetDisplayName("Simple LogParser");
                x.SetServiceName("SimpleLogParser");
            });
        }
    }
}
