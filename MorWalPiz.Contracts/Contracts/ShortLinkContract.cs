using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts
{
    [DataContract]
    public class CategoryContract
    {
        [DataMember]
        public string CategoryId { get; set; } = string.Empty;
        [DataMember]
        public string Title { get; set; } = string.Empty;
        [DataMember]
        public string Description { get; set; } = string.Empty;
    }
    public class QueryLinkContract
    {
        public string QueryLinkId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
    [DataContract]
    public class ShortLinkContract
    {
        [DataMember]
        public string Code { get; set; } = string.Empty;
        [DataMember]
        public string Endpoint { get; set; } = string.Empty;

        [DataMember]
        public string Target { get; set; } = string.Empty;

        [DataMember]
        public string QueryString { get; set; } = string.Empty;

        [DataMember]
        public string ShortLinkId { get; set; } = string.Empty;

        [DataMember]
        public int ClicksCount { get; set; }

        [DataMember]
        public LinkType LinkType { get; set; }

        [DataMember]
        public string VideoId { get; set; } = string.Empty;
    }
}
