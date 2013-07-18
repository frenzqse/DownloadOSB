﻿
using OSBConnection;
using Renci.SshNet;
using SonaTypeDependencies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            SonatypeDependencyManager dm = new SonatypeDependencyManager("https://oss.sonatype.org/content/repositories/snapshots", "org.openengsb.framework", "openengsb-framework", "3.0.0-SNAPSHOT");
            String filename=dm.DownloadZipArtefactToFolder(@"C:\Users\frenz\Desktop");
            string name = dm.UnzipFile(filename);
            OSB openengsb = new OSB(@"C:\Users\frenz\Desktop\openengsb-framework-3.0.0-SNAPSHOT\");
            openengsb.connectToOSBWithSSH();
            List<String> commands = new List<string>();
            commands.Add("feature:install openengsb-domain-example");
            commands.Add("feature:install  openengsb-ports-jms");
            commands.Add("feature:install  openengsb-ports-rs");
            foreach (String command in commands)
            {
                openengsb.executeCommand(command);
            }
            openengsb.closeConnection();
        }

    }
}