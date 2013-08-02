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
    public class OpenEngSBConnection
    {
        private const String openEngSBBatLocation = @"\bin\openengsb.bat";
        private SshClient osbClient;
        private String osbFolderLocation = "";
        private String username = "karaf";
        private String password = "karaf";
        private String OSBUrl = "localhost";
        private int port = 8101;
        private Process osbProcess;
        private static int retries;
        
        public Boolean isOSBConnectionOpen { get; private set; }

        public OpenEngSBConnection(String osbFolderLocation) {
            isOSBConnectionOpen = false;
            this.osbFolderLocation = osbFolderLocation;
            retries = 5;
        }
        public OpenEngSBConnection(String OSBUrl, String username, String password, int port, String osbFolderLocation):this(osbFolderLocation)
        {
            this.OSBUrl = OSBUrl;
            this.username = username;
            this.password = password;
            this.port = port;
        }

        private void startOpenEngSB()
        {
            ProcessStartInfo start = new ProcessStartInfo(osbFolderLocation + openEngSBBatLocation);
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = false;
            start.UseShellExecute = false;
            osbProcess = Process.Start(start);
            //Wait until the OSB is started
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
                //The OSB is not started yet. Retry again
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
        }
    }
}