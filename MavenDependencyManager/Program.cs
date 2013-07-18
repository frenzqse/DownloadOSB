using MavenDependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MavenDependencyManager
{
    class Program
    {
        static void Main(string[] args)
        {
            DependencyManager md = new DependencyManager("http://search.maven.org/", new String[] { "openengsb-framework" });
            md.GetArtifacts();
        }
    }
}
