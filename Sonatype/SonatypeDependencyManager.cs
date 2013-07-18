using Ionic.Zip;
using Org.OpenEngSB.Loom.Csharp.VisualStudio.Plugins.Assistants.Service.Communication.JSON.MavenCentral;
using Sonatype;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SonaTypeDependencies
{
    public class SonatypeDependencyManager
    {
        private const String jsonSearchArtefactParameters = "&wt=json";
        private const String jsonSearchVersionParameter = "core=gav";
        private const String searchURL = "/solrsearch/select?q=";

        private WebClient client = new WebClient();
        private String packageUrl;
        
        public List<SnapshotArtefact> artefacts;
        public SonatypeDependencyManager()
        {
        }
        public SonatypeDependencyManager(string baseUrl, String packageName, String artefactId, String version)
        {
            this.packageUrl = baseUrl;
            foreach (String element in packageName.Split('.'))
            {
                this.packageUrl += "/" + element;
            }
            this.packageUrl += "/" + artefactId + "/";
            this.packageUrl += version + "/";
            String tmp = client.DownloadString(packageUrl);
            artefacts = SnapshotArtefact.getinstance(tmp);
        }

        public String DownloadZipArtefactToFolder(String FolderLocation)
        {
            foreach (SnapshotArtefact artefact in artefacts)
            {
                if (artefact.Name.EndsWith(".zip") && !artefact.Name.Contains("src"))
                {
                    String locationAndName = FolderLocation + "\\" + artefact.Name;
                    client.DownloadFile(artefact.PackageLink, locationAndName);
                    return locationAndName;
                }
            }
            return null;
        }
        public String UnzipFile(String FileLocation)
        {
            if (!File.Exists(FileLocation))
            {
                throw new ArgumentException("File does not exists");
            }
            DirectoryInfo unziptoFileName = new FileInfo(FileLocation).Directory;
            List<String> directories = new List<String>();
            using (ZipInputStream stream = new ZipInputStream(FileLocation))
            {
                ZipEntry e;
                while ((e = stream.GetNextEntry()) != null)
                {
                    if (!File.Exists(e.FileName))
                    {
                        if (e.IsDirectory)
                        {
                            if (!directories.Contains(e.FileName))
                            {
                                directories.Add(e.FileName);
                            }
                            Directory.CreateDirectory(unziptoFileName.ToString() + "\\" + e.FileName);
                            continue;
                        }
                        BinaryReader sr = new BinaryReader(stream);
                        {
                            FileStream fileStream = File.Create(unziptoFileName.ToString() + "\\" + e.FileName);
                            long sizelong = e.UncompressedSize;
                            int size = (int)sizelong;
                            if (sizelong > size)
                            {
                                size = int.MaxValue;
                            }
                            byte[] buffer = new byte[size];
                            while (sr.Read(buffer, 0, size) > 0)
                            {
                                fileStream.Write(buffer, 0, size);
                            }
                        }
                    }
                }
            }
            DirectoryInfo checkExistendDirectory = new DirectoryInfo(unziptoFileName.ToString() + "/" + findToDirectory(directories));
            if (checkExistendDirectory.Exists)
            {
                return checkExistendDirectory.FullName;
            }
            throw new ArgumentException("The OSB directory could not be unzipped successfully");
        }
        private String findToDirectory(List<String> directories)
        {
            int min = -1;
            String topDirectory = null;
            foreach (String directory in directories)
            {
                if (min < 0 || directory.Length < min)
                {
                    min = directory.Length;
                    topDirectory = directory;
                }
            }
            return topDirectory;
        }
    }
}