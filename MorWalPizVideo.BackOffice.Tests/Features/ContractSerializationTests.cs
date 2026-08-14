using System.Text.Json;
using MorWalPiz.Contracts;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ContractSerializationTests
{
  [Fact]
  public void Channel_wire_contract_serializes_exact_isSHIT_name()
  {
    var channel = new YTChannel("channel", "Shooting") { IsSHIT = true };
    var contract = ContractUtils.Convert(channel);

    var json = JsonSerializer.Serialize(contract);

    Assert.Contains("\"isSHIT\":true", json, StringComparison.Ordinal);
    Assert.DoesNotContain("\"isShit\"", json, StringComparison.Ordinal);
  }

  [Fact]
  public void Channel_model_serializes_exact_isSHIT_name()
  {
    var channel = new YTChannel("channel", "Shooting") { IsSHIT = true };

    var json = JsonSerializer.Serialize(channel);

    Assert.Contains("\"isSHIT\":true", json, StringComparison.Ordinal);
    Assert.DoesNotContain("\"isShit\"", json, StringComparison.Ordinal);
  }

  [Fact]
  public void Legacy_channel_defaults_isSHIT_to_false()
  {
    var channel = new YTChannel("channel", "Legacy");

    Assert.False(channel.IsSHIT);
  }
}