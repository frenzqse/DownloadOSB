using Ionic.Zip;
using log4net;
using log4net.Repository.Hierarchy;
using Sonatype;
using Sonatype.SearchResultXmlStructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace SonaTypeDependencies
{
    public class SonatypeDependencyManager
    {
        private static ILog logger = LogManager.GetLogger(typeof(SonatypeDependencyManager));
        private const String BaseUrl="http://repository.sonatype.org/";
        private const String SearchUrl="service/local/data_index?";
        private const String GroupSearchParameter="g={0}";
        private const String ArtefactSearchParameter="&a={0}";
        private const String PackageSearchParameter="&p={0}";
        private const String ExtensionSearchParameter="&e={0}";
        private const String VersionSearchParameter="&v={0}";

        private String groupId;
        private String artefactId;
        private String version;
        private String packaging;
        private String classifier;
 
        private WebClient client = new WebClient();
        
        public SonatypeDependencyManager()
        {
        }

        public SonatypeDependencyManager(String groupId, String artefactId, String version,String packaging,String classifier)
        {
            this.groupId = groupId;
            this.artefactId = artefactId;
            this.version = version;
            this.packaging = packaging;
            this.classifier = classifier;
        }

        private String getSearchUrl()
        {
            if (String.IsNullOrEmpty(groupId) || String.IsNullOrEmpty(packaging))
            {
                throw new ArgumentException("The packageName and packaging can not be empty");
            }
            StringBuilder builder = new StringBuilder(BaseUrl);
            builder.Append(SearchUrl);
            builder.Append(String.Format(GroupSearchParameter,groupId));
            if (IsStringNotEmpty(artefactId))
            {
                builder.Append(String.Format(ArtefactSearchParameter, artefactId));
            }
            if (IsStringNotEmpty(version))
            {
                builder.Append(String.Format(VersionSearchParameter, version));
            }
            builder.Append(String.Format(PackageSearchParameter, packaging));
            return builder.ToString();
        }

        private bool IsStringNotEmpty(String value)
        {
            return !String.IsNullOrEmpty(value);
        }
        public String DownloadZipArtefactToFolder(String FolderLocation)
        {
            String searchResult = client.DownloadString(new Uri(getSearchUrl()));
            SearchResult result = ConvertSearchResult<SearchResult>(searchResult);
            result.artefacts.AddRange(findArtifactOverRest());
            Artifact selectedArtifact = findCorrectArtefact(result);
            String completeFileName = FolderLocation + "\\" + selectedArtifact.ArtifactId + "-" + selectedArtifact.Version + "." + selectedArtifact.Packaging;
            client.DownloadFile(selectedArtifact.ArtifactLink, completeFileName);

            return completeFileName;
        }
        private List<Artifact> findArtifactOverRest()
        {
            StringBuilder urlBuilder = new StringBuilder("https://oss.sonatype.org/content/repositories/snapshots");
            foreach (String element in groupId.Split('.'))
            {
                urlBuilder.Append("/");
                urlBuilder.Append(element);
            }
            urlBuilder.Append("/");
            urlBuilder.Append( artefactId);
            urlBuilder.Append("/");
            urlBuilder.Append(version);
            urlBuilder.Append("/");
            return HtmlAtifact.getinstance(client.DownloadString(urlBuilder.ToString()));
        }
        private List<Artifact> removeWrongArtefacts(List<Artifact> artefacts)
        {
            return artefacts.FindAll(ar =>
            {
                return ar.ArtifactId == this.artefactId
                    && ar.Classifier == this.classifier
                    && ar.Packaging == this.packaging;
            });
        }

        private Artifact findCorrectArtefact(SearchResult result)
        {
            List<Artifact> matchingArtefacts = removeWrongArtefacts(result.artefacts);
            if (matchingArtefacts.Count >= 1)
            {
                return matchingArtefacts[0];
            }
            return null;
        }
        public ResultType ConvertSearchResult<ResultType>(String searchResult)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ResultType));
            StringReader reader = new StringReader(searchResult);
            return (ResultType)serializer.Deserialize(reader);
        }

        
        public String UnzipFile(String FileLocation)
        {
            if (!File.Exists(FileLocation))
            {
                throw new ArgumentException("File does not exists");
            }
            DirectoryInfo unziptoFileName = new FileInfo(FileLocation).Directory;
            List<String> directories = new List<String>();
            logger.Info("Unzipping the zip to the folder " + FileLocation);
            using (ZipInputStream stream = new ZipInputStream(FileLocation))
            {
                logger.Info("Elements to processed: " + stream.Length);
                int i = 0;
                ZipEntry e;
                while ((e = stream.GetNextEntry()) != null)
                {
                    logger.Info("Completed: " + i++ + "/" + stream.Length);
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
            DirectoryInfo checkExistendDirectory = new DirectoryInfo(unziptoFileName.ToString() + "/" + findTopDirectory(directories));
            logger.Info("Unzip completed");
            if (checkExistendDirectory.Exists)
            {
                return checkExistendDirectory.FullName;
            }
            throw new ArgumentException("The OSB directory could not be unzipped successfully");
        }

        private String findTopDirectory(List<String> directories)
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