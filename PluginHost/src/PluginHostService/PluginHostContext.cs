using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PluginHostService
{
    public class PluginHostContext
    {
        public PluginRepository PluginRepo { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

    }
}
