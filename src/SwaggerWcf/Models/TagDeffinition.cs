using System.Runtime.Serialization;

namespace SwaggerWcf.Models
{
    [DataContract]
    public class TagDeffinition
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        public int SortOrder { get; set; }
    }
}