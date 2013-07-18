using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MavenDependencies
{
    public class Item
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public string DllPath { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public ItemVersion ParentVersion { get; set; }

        public Item(string name, string url, ItemVersion version)
        {
            Name = name;
            Url = url;
            ParentVersion = version;
        }
    }
}
