using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts
{
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
