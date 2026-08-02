using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class PublishScheduleContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string VideoId { get; set; } = string.Empty;

    [DataMember]
    public string[] QueryStringIds { get; set; } = [];

    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public DateTime Date { get; set; }
}