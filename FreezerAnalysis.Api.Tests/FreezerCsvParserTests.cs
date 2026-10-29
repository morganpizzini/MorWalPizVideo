using System.Text;
using FreezerAnalysis.Api.Services;
using MorWalPizVideo.Models.FreezerAnalysis;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerCsvParserTests
{
    private readonly FreezerCsvParser parser = new();

    [Fact]
    public void Score_csv_reconciles_totals_and_classifies_complete_import_as_final()
    {
        var parsed = parser.ParseScores(
            Encoding.UTF8.GetBytes(FinalScoreCsv),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None);

        Assert.Equal("match-1", parsed.MatchKey);
        Assert.Equal(2, parsed.Stages.Count);
        Assert.Equal(2, parsed.Results.Count);
        Assert.Equal(FreezerImportCompleteness.Final, parsed.Completeness);
        Assert.Contains("competitor-1", parsed.AvailableBeforeStartCompetitorIds);
        Assert.Contains("2", parsed.ExcludedStageIds);
        Assert.Equal(5, parsed.Results[0].Stages[0].NetPoints);
    }

    [Fact]
    public void Score_csv_keeps_missing_classification_as_provisional()
    {
        var csv = FinalScoreCsv.Replace(",10,", ",,", StringComparison.Ordinal);
        var parsed = parser.ParseScores(
            Encoding.UTF8.GetBytes(csv),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None);

        Assert.Equal("Provisional", parsed.Completeness.ToString());
        Assert.Contains(parsed.Diagnostics, item => item.Contains("provisional", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Score_csv_rejects_mismatched_match_dates_before_publication()
    {
        Assert.Throws<FormatException>(() => parser.ParseScores(
            Encoding.UTF8.GetBytes(FinalScoreCsv),
            "Gara di prova",
            new DateOnly(2026, 10, 30),
            CancellationToken.None));
    }

    [Fact]
    public void Roster_csv_returns_unresolved_entries_without_guessing_identity()
    {
        const string roster = """
            gara,comp_id,nome,div,squad,cls,fattore,cat,giorno
            match-1,competitor-1,Atleta Uno,Production,3,5,Minor,Senior,sabato
            """;

        var preview = parser.ParseRoster(Encoding.UTF8.GetBytes(roster), CancellationToken.None);

        Assert.Equal(1, preview.CompetitorCount);
        Assert.Equal(1, preview.UnresolvedCount);
        Assert.Equal("3", preview.Entries[0].Squad);
        Assert.Contains(preview.Diagnostics, item => item.Contains("unresolved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Csv_rejects_duplicate_competitor_stage_results()
    {
        var csv = FinalScoreCsv + Environment.NewLine + FinalScoreCsv.Split('\n')[1];

        Assert.Throws<FormatException>(() => parser.ParseScores(
            Encoding.UTF8.GetBytes(csv),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None));
    }

    [Theory]
    [InlineData("Major", 4)]
    [InlineData("Minor", 3)]
    public void Net_points_use_power_factor_scoring(string powerFactor, int expected)
    {
        var source = SingleScoreCsv(powerFactor, miss: 0, noShoot: 0);
        var parsed = parser.ParseScores(
            Encoding.UTF8.GetBytes(source),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None);

        Assert.Equal(expected, Assert.Single(Assert.Single(parsed.Results).Stages).NetPoints);
    }

    [Fact]
    public void Net_points_apply_penalties_and_clamp_at_zero()
    {
        var source = SingleScoreCsv("Minor", miss: 1, noShoot: 1);
        var parsed = parser.ParseScores(
            Encoding.UTF8.GetBytes(source),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None);

        Assert.Equal(0, Assert.Single(Assert.Single(parsed.Results).Stages).NetPoints);
    }

    [Fact]
    public void Semicolon_csv_parses_decimal_comma()
    {
        const string csv = """
            gara;data;comp_id;nome;div;fattore;stage;colpi;hf;punti;tempo;totale;prima;escluso;azzerato;dq;A;C;D;M;NS;PE
            match-1;29/10/2026;competitor-1;Atleta Uno;Production;Major;1;1;1,5;1,0;2,0;1,0;true;false;false;false;0;1;0;0;0;0
            """;

        var parsed = parser.ParseScores(
            Encoding.UTF8.GetBytes(csv),
            "Gara di prova",
            new DateOnly(2026, 10, 29),
            CancellationToken.None);

        Assert.Equal(1.5, Assert.Single(Assert.Single(parsed.Results).Stages).HitFactor);
        Assert.Equal(4, Assert.Single(Assert.Single(parsed.Results).Stages).NetPoints);
    }

    private static string SingleScoreCsv(string powerFactor, int miss, int noShoot) =>
        $"""
        gara,data,comp_id,nome,div,fattore,stage,colpi,hf,punti,tempo,totale,prima,escluso,azzerato,dq,A,C,D,M,NS,PE
        match-1,29/10/2026,competitor-1,Atleta Uno,Production,{powerFactor},1,1,1,1,1,1,true,false,false,false,0,1,0,{miss},{noShoot},0
        """;

    private const string FinalScoreCsv = """
        gara,data,comp_id,nome,div,fattore,stage,colpi,hf,punti,tempo,totale,prima,escluso,azzerato,dq,A,C,D,M,NS,PE
        match-1,29/10/2026,competitor-1,Atleta Uno,Production,Minor,1,1,1,5,5,10,true,false,false,false,1,0,0,0,0,0
        match-1,29/10/2026,competitor-1,Atleta Uno,Production,Minor,2,1,1,5,5,10,false,true,false,false,1,0,0,0,0,0
        match-1,29/10/2026,competitor-2,Atleta Due,Production,Minor,1,1,1,5,5,10,false,false,false,false,1,0,0,0,0,0
        match-1,29/10/2026,competitor-2,Atleta Due,Production,Minor,2,1,1,5,5,10,false,false,false,false,1,0,0,0,0,0
        """;
}
