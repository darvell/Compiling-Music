using System;
using System.Collections.Generic;
using System.Text;
using Extensibility;
using EnvDTE;
using EnvDTE80;

// Let's just keep the ugly mandatory things in here.

namespace CompilingMusic
{
    public class Connect : IDTExtensibility2
    {
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }
    }
}
