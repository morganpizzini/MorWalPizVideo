using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class CalendarEventContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;

    [DataMember]
    public DateTime StartDate { get; set; }

    [DataMember]
    public DateTime EndDate { get; set; }

    [DataMember]
    public CategoryRef[] Categories { get; set; } = [];

    [DataMember]
    public string MatchId { get; set; } = string.Empty;

    [DataMember]
    public string MatchUrl { get; set; } = string.Empty;

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;
}