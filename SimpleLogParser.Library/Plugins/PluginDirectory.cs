using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SimpleLogParser.Common;

namespace SimpleLogParser.Common
{
    public class PluginDirectory
    {
        public PluginDirectory() {}
        
        public List<IParserPlugin> AllPlugins()
        {
            var executablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var catalog = new DirectoryCatalog(Path.Combine(executablePath, "plugins"));

            var container = new CompositionContainer(catalog);
            return container.GetExportedValues<IParserPlugin>().ToList();
        }
    }
}
