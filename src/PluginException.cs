using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaActorTesting
{
    public class PluginException : Exception
    {
        public PluginException() : base()
        {

        }

        public PluginException(string message) : base(message)
        {

        }
    }
}
