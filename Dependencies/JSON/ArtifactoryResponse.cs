using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.OpenEngSB.Loom.Csharp.VisualStudio.Plugins.Assistants.Service.Communication.JSON
{
    public class ArtifactoryResponse
    {
        public Entry[] results;

        public class Entry
        {
            public string uri;
        }
    }
}
