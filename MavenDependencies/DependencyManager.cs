using Org.OpenEngSB.Loom.Csharp.VisualStudio.Plugins.Assistants.Service.Communication.JSON.MavenCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MavenDependencies
{
    public class DependencyManager
    {
        private const String jsonSearchArtefactParameters="&wt=json";
        private const String jsonSearchVersionParameter = "core=gav";
        private const String searchURL="/solrsearch/select?q=";

        private WebClient client = new WebClient();
        private string baseUrl;
        private string fileBaseUrl;
        private JavaScriptSerializer serializer = new JavaScriptSerializer();
        private string[] groupIds;


        public DependencyManager(string baseUrl, string[] groupIds)
        {
            this.baseUrl = baseUrl + searchURL;
            this.fileBaseUrl = "http://search.maven.org/remotecontent?filepath=";
            this.groupIds = groupIds;
        }

        private string getArtifactsUrl(string groupId)
        {
            return baseUrl + getArtifactsQuery(groupId) + jsonSearchArtefactParameters;
        }

        private string getVersionsUrl(string groupId, string artifactId)
        {
            return baseUrl + getVersionsQuery(groupId, artifactId) + jsonSearchArtefactParameters+"&"+jsonSearchVersionParameter;
        }

        private string getArtifactsQuery(string groupId)
        {
            return "g:\"" + groupId + "\"";
        }

        private string getVersionsQuery(string groupId, string artifactId)
        {
            return getArtifactsQuery(groupId) + "+" + "AND" + "+" + "a:" + "\"" + artifactId + "\"";
        }

        public IList<Artifact> GetArtifacts()
        {
            List<Artifact> artifacts = new List<Artifact>();
            foreach (string groupId in groupIds)
            {
                IList<Artifact> a = GetArtifacts(groupId);
                artifacts.AddRange(a);
            }
            return artifacts;
        }

        public IList<Artifact> GetArtifacts(string groupId)
        {
            string url = getArtifactsUrl(groupId);
            string artifactsString = client.DownloadString(url);
            ArtifactsListing al = serializer.Deserialize<ArtifactsListing>(artifactsString);

            IList<Artifact> artifacts = new List<Artifact>();
            foreach (ArtifactsDoc doc in al.response.docs)
            {
                Artifact artifact = new Artifact(groupId, doc.a);
                IList<ItemVersion> versions = GetVersions(groupId, artifact);
                artifact.Versions = versions;
                if(versions.Count > 0)
                    artifacts.Add(artifact);
            }
            return artifacts;
        }

        public IList<ItemVersion> GetVersions(string groupId, Artifact artifact)
        {
            string url = getVersionsUrl(groupId, artifact.Id);
            string versionsString = client.DownloadString(url);
            VersionsListing vl = serializer.Deserialize<VersionsListing>(versionsString);
            IList<ItemVersion> versions = new List<ItemVersion>();

            foreach (VersionsDoc doc in vl.response.docs)
            {
                ItemVersion iv = new ItemVersion(doc.v, artifact);
                iv.Items = getItems(iv, doc.ec);
                if (iv.Items.Count > 0)
                    versions.Add(iv);
            }
            return versions;
        }

        private IList<Item> getItems(ItemVersion iv, string[] files)
        {
            IList<Item> items = new List<Item>();
            foreach (string file in files)
            {
                if(true)
                {
                    String url = generateFileUrl(file, iv);
                    Item i = new Item(generateFileName(file, iv),url, iv);
                    items.Add(i);
                }
            }
            return items;
        }

        private string generateFileUrl(string fileName, ItemVersion version)
        {
            string parameter = version.ParentArtifact.GroupId.Replace(".", "/");
            parameter += "/" + version.ParentArtifact.Id;
            parameter += "/" + version.Id;
            parameter += "/" + generateFileName(fileName, version);
            return fileBaseUrl + parameter;
        }

        private string generateFileName(string fileName, ItemVersion version)
        {
            return version.ParentArtifact.Id + "-" + version.Id + fileName;
        }

        public string GetItemDownloadUrl(string groupdId, string artifactId, string itemName)
        {
            return groupdId + artifactId + itemName;
        }
    }
}
