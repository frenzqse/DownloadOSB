using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sonatype.SearchResultXmlStructure
{
    [Serializable]
    [XmlType("search-results")]
    public class SearchResult : System.Object
    {
        [XmlElement("totalCount")]
        public int TotalCount { get; set; }
        [XmlElement("from")]
        public String From { get; set; }
        [XmlElement("count")]
        public int Count { get; set; }
        [XmlElement("tooManyResults")]
        public Boolean TooManyResults { get; set; }
        [XmlArray("data")]
        public List<Artifact> artefacts { get; set; }

        public SearchResult() {
            artefacts = new List<Artifact>();
        }
        public override Boolean Equals(Object artifact1)
        {
            if (!artifact1.GetType().Equals(this.GetType()))
            {
                return false;
            }
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo field in fields)
            {
                if (AreNotEqual(field.GetValue(this), field.GetValue(artifact1)))
                {
                    return false;
                }
            }
            return true;
        }

        private Boolean AreNotEqual(Object obj1Value, Object obj2Value)
        {
            if (obj2Value != null && !obj2Value.Equals(obj1Value))
            {
                return true;
            }
            else if (obj1Value != null && !obj1Value.Equals(obj2Value))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
