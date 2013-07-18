using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSBConnection
{
    public class OSB
    {
        private SshClient osbClient;
        private String osbFolderLocation = "";
        private String username = "karaf";
        private String password = "karaf";
        private String OSBUrl = "localhost";
        private int port = 8101;
        private Process osbProcess;
        private static int retries;
        private Process[] startJavaprocesses;
        public Boolean isOSBConnectionOpen { get; private set; }
        public OSB(String osbFolderLocation) {
            isOSBConnectionOpen = false;
            this.osbFolderLocation = osbFolderLocation;
            retries = 5;
            startJavaprocesses= Process.GetProcessesByName("java");
        }
        public OSB(String OSBUrl, String username, String password, int port, String osbFolderLocation):this(osbFolderLocation)
        {
            this.OSBUrl = OSBUrl;
            this.username = username;
            this.password = password;
            this.port = port;
        }
        private void startOpenEngSB()
        {
            ProcessStartInfo start = new ProcessStartInfo(osbFolderLocation + @"\bin\openengsb.bat");
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = false;
            start.UseShellExecute = false;
            osbProcess = Process.Start(start);
            Thread.Sleep(5000);
        }
        public void connectToOSBWithSSH()
        {
            isOSBConnectionOpen = false;
            startOpenEngSB();
            osbClient = new SshClient(OSBUrl, port, username, password);
            try
            {
                osbClient.Connect();
            }
            catch (SocketException e)
            {
                if (retries-- <= 0)
                {
                    throw e;
                }
                Thread.Sleep(5000);
                connectToOSBWithSSH();
            }
            isOSBConnectionOpen = true;
        }
        public void executeCommand(String command)
        {
            osbClient.RunCommand(command);
        }
        public void closeConnection()
        {
            isOSBConnectionOpen = false;
            executeCommand("shutdown -f");
            Thread.Sleep(10000);
            osbClient.Disconnect();
            osbProcess.WaitForExit(10000);
            Process[] currentjavaprocesses = Process.GetProcessesByName("java");
            foreach (Process process in currentjavaprocesses)
            {
                if (!startJavaprocesses.Contains(process))
                {
                    process.WaitForExit();
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
        }
    }
}