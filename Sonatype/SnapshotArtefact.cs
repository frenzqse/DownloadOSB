using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sonatype
{
    public class SnapshotArtefact
    {
        private const String linkEndTak="</a>";
        public String Name { get; set; }
        public String PackageLink { get; set; }

        public SnapshotArtefact() { }
        public SnapshotArtefact(String href)
        {
            int start = href.IndexOf("=") + 1;
            int end = href.IndexOf(">");
            String url = href.Substring(start, end - start);
            String tmpname=href.Substring(end+1);
            this.PackageLink = url.Replace("\"", "");
            this.Name = tmpname.Substring(0, tmpname.IndexOf("<"));

        }
        public static List<SnapshotArtefact> getinstance(String htmlpage)
        {
            List<SnapshotArtefact> result = new List<SnapshotArtefact>();
            String intermediatResult = "";
            SnapshotArtefact sa = new SnapshotArtefact();
            String tmp = htmlpage;
            while (tmp.Contains("<a href"))
            {
                int end = tmp.IndexOf(linkEndTak) + linkEndTak.Length;
                int start = tmp.IndexOf("<a href=\"");
                intermediatResult = tmp.Substring(start,end-start);
                tmp = tmp.Substring(end);
                result.Add(new SnapshotArtefact(intermediatResult));
            }
            return result;
        }
    }
}
