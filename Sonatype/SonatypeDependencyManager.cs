using Ionic.Zip;
using log4net;
using Sonatype;
using Sonatype.SearchResultXmlStructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using ZetaLongPaths;

namespace SonaTypeDependencies
{
    public class SonatypeDependencyManager
    {
        private static ILog logger = LogManager.GetLogger(typeof(SonatypeDependencyManager));
        private const String BaseUrl = "http://repository.sonatype.org/";
        private const String RestFullBaseUrl = "https://oss.sonatype.org/content/repositories/snapshots";
        private const String SearchUrl = "service/local/data_index?";
        private const String GroupSearchParameter = "g={0}";
        private const String ArtefactSearchParameter = "&a={0}";
        private const String PackageSearchParameter = "&p={0}";
        private const String ExtensionSearchParameter = "&e={0}";
        private const String VersionSearchParameter = "&v={0}";

        private String groupId;
        private String artefactId;
        private String version;
        private String packaging;
        private String classifier;

        private WebClient client = new WebClient();
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="artefactId">Artefact ID</param>
        /// <param name="version">Version</param>
        /// <param name="packaging">Packaging</param>
        /// <param name="classifier">Classifier</param>
        public SonatypeDependencyManager(String groupId, String artefactId, String version, String packaging, String classifier)
        {
            this.groupId = groupId;
            this.artefactId = artefactId;
            this.version = version;
            this.packaging = packaging;
            this.classifier = classifier;
        }

        /// <summary>
        /// Creates a Url from the groupId, artefactId and the Version
        /// </summary>
        /// <returns></returns>
        private String getSearchUrl()
        {
            logger.Info("Generate Search Url");
            if (String.IsNullOrEmpty(groupId) || String.IsNullOrEmpty(artefactId)
                || String.IsNullOrEmpty(packaging))
            {
                throw new ArgumentException("The packageName, version or packaging can not be empty");
            }
            StringBuilder builder = new StringBuilder(BaseUrl);
            builder.Append(SearchUrl);
            builder.Append(String.Format(GroupSearchParameter, groupId));
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

        /// <summary>
        /// Downloads the Artifact according to the parameters that has been
        /// indicated over the constructor
        /// </summary>
        /// <param name="FolderLocation">The location where the Artefact should be dowloaded</param>
        /// <returns>The Absolute Path to the Artefact</returns>
        public String DownloadArtefactToFolder(String FolderLocation)
        {
            logger.Info("Download all the Artefacts (In XML format)");
            String searchResult = client.DownloadString(new Uri(getSearchUrl()));
            logger.Info("Convert the XML to SearchResult object");
            SearchResult result = ConvertSearchResult<SearchResult>(searchResult);
            logger.Info("Add the Artefacts by search the artefacts (Because of a Bug in Nexus sonatype)");
            result.artefacts.AddRange(findArtifactOverRest());
            logger.Info("Search for the artefact that fulfils all the criteria");
            Artifact selectedArtifact = findCorrectArtefact(result);
            String FolderLocationAndFileName = FolderLocation + @"\" + selectedArtifact.ArtifactId + "-" + selectedArtifact.Version + "." + selectedArtifact.Packaging;
            logger.Info("Download the Artefact to the folder " + FolderLocationAndFileName);
            client.DownloadFile(selectedArtifact.ArtifactLink, FolderLocationAndFileName);
            return FolderLocationAndFileName;
        }

        /// <summary>
        /// Nexus sonatype does not list elements that have no classifier.
        /// To find zip files (wiht no classifier) this workaround is used.
        /// (Search the file over a Rest url)
        /// </summary>
        /// <returns></returns>
        private List<Artifact> findArtifactOverRest()
        {
            StringBuilder urlBuilder = new StringBuilder(RestFullBaseUrl);
            foreach (String element in groupId.Split('.'))
            {
                urlBuilder.Append("/");
                urlBuilder.Append(element);
            }
            urlBuilder.Append("/");
            urlBuilder.Append(artefactId);
            urlBuilder.Append("/");
            urlBuilder.Append(version);
            urlBuilder.Append("/");
            return HtmlAtifact.getinstance(client.DownloadString(urlBuilder.ToString()));
        }
        /// <summary>
        /// Returns all the artefacts that fullfils the parameters (indicated over the constructor)
        /// </summary>
        /// <param name="artefacts"></param>
        /// <returns></returns>
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
            if (matchingArtefacts.Count > 0)
            {
                return matchingArtefacts[matchingArtefacts.Count-1];
            }
            throw new ArgumentException("For the given parameters, no Artefact could be found");
        }
        /// <summary>
        /// Converts the SearchResult (in XML) to an Object
        /// </summary>
        /// <typeparam name="ResultType"></typeparam>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        public ResultType ConvertSearchResult<ResultType>(String searchResult)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ResultType));
            StringReader reader = new StringReader(searchResult);
            return (ResultType)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Unzips a zip to a folder
        /// </summary>
        /// <param name="FileLocation">The folder where the zip should be located</param>
        /// <returns></returns>
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
                logger.Info("Start unzipping");
                ZipEntry e;
                while ((e = stream.GetNextEntry()) != null)
                {
                    logger.Info("Completed: " + stream.Position + "/" + stream.Length);
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
                            String tmp = unziptoFileName.ToString() + "\\" + e.FileName;
                            ZlpFileInfo file = new ZlpFileInfo(tmp);
                            ZlpDirectoryInfo directory = file.Directory;
                            if (!ZlpIOHelper.DirectoryExists(directory.FullName))
                            {
                                directory.Create();
                            }
                            Directory.SetCurrentDirectory(directory.FullName);
                            int a = file.Name.Length;
                            long sizelong = e.UncompressedSize;
                            int size = (int)sizelong;
                            if (sizelong > size)
                            {
                                size = int.MaxValue;
                            }
                            byte[] buffer = new byte[size];
                            List<byte> asd = new List<byte>();
                            while (sr.Read(buffer, 0, size) > 0)
                            {
                                asd.AddRange(buffer);
                            }
                            try
                            {
                                ZlpIOHelper.WriteAllBytes(file.FullName, asd.ToArray<byte>());
                            }
                            catch
                            {
                                logger.Info("Path to long exception has been regognized. Create tmp file and rename this one");
                                ZlpFileInfo tmpFile = new ZlpFileInfo(file.Directory + @"/tmp"+(new Random()).Next());
                                ZlpIOHelper.WriteAllBytes(tmpFile.FullName, asd.ToArray<byte>());
                                if (Microsoft.Experimental.IO.LongPathFile.Exists(tmpFile.FullName))
                                {
                                    Microsoft.Experimental.IO.LongPathFile.Delete(file.FullName);
                                }
                                Microsoft.Experimental.IO.LongPathFile.Move(tmpFile.FullName, file.FullName);
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

        private void dast(String zip, string result)
        {
            Shell32.Shell sc = new Shell32.Shell();
            Shell32.Folder SrcFlder = sc.NameSpace(zip);
            Shell32.Folder DestFlder = sc.NameSpace(result);
            Shell32.FolderItems items = SrcFlder.Items();
            DestFlder.CopyHere(items, 4);
        }
        /// <summary>
        /// Searches the Top folder from a List of folders 
        /// </summary>
        /// <param name="directories"></param>
        /// <returns></returns>
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