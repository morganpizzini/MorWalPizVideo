using System.Text.Json;
using MorWalPiz.Contracts;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:Shared")]
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

  [Fact]
  public void Legacy_custom_form_null_arrays_serialize_as_empty_arrays()
  {
    var question = new MultipleChoiceQuestion("question", "Question", true, 1, [])
    {
      Options = null!
    };
    var singleChoiceQuestion = new SingleChoiceQuestion("single-question", "Question", true, 2, [])
    {
      Options = null!
    };
    var answer = new MultipleChoiceAnswer("question", null!);
    answer = answer with { SelectedOptionIds = null! };
    var response = new CustomFormResponse("response", DateTime.UtcNow, [])
    {
      Answers = null!
    };
    var form = new CustomForm("Form", "Description", "form", [question])
    {
      Questions = null!,
      Responses = null!
    };

    var json = JsonSerializer.Serialize(form);

    Assert.Empty(form.Questions);
    Assert.Empty(form.Responses);
    Assert.Equal(0, form.ResponseCount);
    Assert.Empty(question.Options);
    Assert.Empty(singleChoiceQuestion.Options);
    Assert.Empty(response.Answers);
    Assert.Empty(answer.SelectedOptionIds);
    Assert.Contains("\"Questions\":[]", json, StringComparison.Ordinal);
    Assert.Contains("\"Responses\":[]", json, StringComparison.Ordinal);
    Assert.Contains("\"Options\":[]", JsonSerializer.Serialize(question), StringComparison.Ordinal);
    Assert.Contains("\"Options\":[]", JsonSerializer.Serialize(singleChoiceQuestion), StringComparison.Ordinal);
    Assert.Contains("\"Answers\":[]", JsonSerializer.Serialize(response), StringComparison.Ordinal);
    Assert.Contains("\"SelectedOptionIds\":[]", JsonSerializer.Serialize(answer), StringComparison.Ordinal);
  }
}