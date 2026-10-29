using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace FreezerAnalysis.Api.Services;

public sealed record FreezerCardStage(
    int StageNumber,
    int MinimumRounds,
    double? Center,
    double? Lower,
    double? Upper,
    string? Strategy);

public sealed record FreezerCardPayload(
    string MatchName,
    string LocalMatchDate,
    string Division,
    string PowerFactor,
    IReadOnlyList<FreezerCardStage> Stages,
    int? StartStage,
    int? StartTurn,
    int? SquadSize,
    IReadOnlyList<string> DisplayOrder,
    IReadOnlyList<string> FieldUpdates);

public sealed record FreezerRenderedDocuments(
    byte[] A5Pdf,
    byte[] A4FoldPdf,
    byte[] PhoneFrontPng,
    byte[] PhoneBackPng,
    string RendererVersion);

public sealed class FreezerDocumentRenderer
{
    public const string RendererVersion = "playwright-1.58.0-card-v1";
    public const int PhoneWidth = 1290;
    public const int PhoneHeight = 2796;
    private const int LogicalWidth = 430;
    private const int LogicalHeight = 932;
    private const int DeviceScale = 3;
    private static readonly SemaphoreSlim BrowserSlots = new(2, 2);

    public async Task<FreezerRenderedDocuments> RenderAsync(
        FreezerCardPayload payload,
        CancellationToken cancellationToken)
    {
        ValidatePayload(payload);
        await BrowserSlots.WaitAsync(cancellationToken);
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--disable-dev-shm-usage", "--no-sandbox"]
            });
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = LogicalWidth, Height = LogicalHeight },
                DeviceScaleFactor = DeviceScale,
                Locale = "it-IT",
                TimezoneId = "Europe/Rome"
            });
            await context.RouteAsync("**/*", route =>
            {
                var scheme = new Uri(route.Request.Url).Scheme;
                return scheme is "about" or "data"
                    ? route.ContinueAsync()
                    : route.AbortAsync();
            });

            var split = (payload.Stages.Count + 1) / 2;
            var frontStages = payload.Stages.Take(split).ToArray();
            var backStages = payload.Stages.Skip(split).ToArray();
            var a5Html = BuildPrintHtml(payload, frontStages, backStages, "a5");
            var a4Html = BuildPrintHtml(payload, frontStages, backStages, "a4");
            var a5Page = await context.NewPageAsync();
            await a5Page.SetContentAsync(a5Html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
            await AssertNoOverflowAsync(a5Page, ".a5-page", cancellationToken);
            var a5Pdf = await a5Page.PdfAsync(new PagePdfOptions
            {
                Format = "A5",
                PrintBackground = true,
                PreferCSSPageSize = true
            });

            var a4Page = await context.NewPageAsync();
            await a4Page.SetContentAsync(a4Html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
            await AssertNoOverflowAsync(a4Page, ".a4-sheet", cancellationToken);
            var a4Pdf = await a4Page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                Landscape = true,
                PrintBackground = true,
                PreferCSSPageSize = true
            });

            var frontPage = await context.NewPageAsync();
            await frontPage.SetContentAsync(BuildPhoneHtml(payload, frontStages, true), new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.Load
            });
            await AssertNoOverflowAsync(frontPage, ".phone-safe-area", cancellationToken);
            var frontPng = await frontPage.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png });

            var backPage = await context.NewPageAsync();
            await backPage.SetContentAsync(BuildPhoneHtml(payload, backStages, false), new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.Load
            });
            await AssertNoOverflowAsync(backPage, ".phone-safe-area", cancellationToken);
            var backPng = await backPage.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png });

            ValidatePdf(a5Pdf);
            ValidatePdf(a4Pdf);
            ValidatePhonePng(frontPng);
            ValidatePhonePng(backPng);
            return new(a5Pdf, a4Pdf, frontPng, backPng, RendererVersion);
        }
        finally
        {
            BrowserSlots.Release();
        }
    }

    private static void ValidatePayload(FreezerCardPayload payload)
    {
        if (payload.Stages.Count is < 1 or > 34)
            throw new InvalidDataException("A card must contain between 1 and 34 stages.");
        if (payload.Stages.Select(x => x.StageNumber).Distinct().Count() != payload.Stages.Count)
            throw new InvalidDataException("A card cannot contain duplicate stages.");
        if (payload.Stages.Any(x => x.MinimumRounds is < 1 or > 100 ||
                                    x.Center is { } center && (!double.IsFinite(center) || center < 0) ||
                                    x.Lower is { } lower && (!double.IsFinite(lower) || lower < 0) ||
                                    x.Upper is { } upper && (!double.IsFinite(upper) || upper < 0)))
            throw new InvalidDataException("A stage has invalid values.");
        foreach (var strategy in payload.Stages.Select(x => x.Strategy).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (strategy!.Length > 260 || Regex.Split(strategy, @"(?<=[.!?])\s+").Count(x => !string.IsNullOrWhiteSpace(x)) > 2)
                throw new InvalidDataException("Each stage strategy is limited to two sentences and 260 characters.");
        }
    }

    private static void ValidatePdf(byte[] bytes)
    {
        if (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            throw new InvalidDataException("Playwright returned an invalid PDF artifact.");
    }

    private static void ValidatePhonePng(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 24 ||
            !bytes.AsSpan(0, signature.Length).SequenceEqual(signature) ||
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)) != PhoneWidth ||
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)) != PhoneHeight)
            throw new InvalidDataException($"Playwright must return an exact {PhoneWidth}x{PhoneHeight} PNG.");
    }

    private static async Task AssertNoOverflowAsync(
        IPage page,
        string selector,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var overflows = await page.EvaluateAsync<bool>(
            """
            selector => {
                const element = document.querySelector(selector);
                if (!element) return true;
                return element.scrollHeight > element.clientHeight + 1 ||
                       element.scrollWidth > element.clientWidth + 1;
            }
            """,
            selector);
        if (overflows)
            throw new InvalidDataException("Card content exceeds the declared readable area; content was not shrunk or truncated.");
    }

    private static string BuildPrintHtml(
        FreezerCardPayload payload,
        IReadOnlyList<FreezerCardStage> front,
        IReadOnlyList<FreezerCardStage> back,
        string format)
    {
        var title = Encode(payload.MatchName);
        var header = $"<header><h1>{title}</h1><p>{Encode(payload.LocalMatchDate)} · {Encode(payload.Division)} · {Encode(payload.PowerFactor)}</p></header>";
        if (format == "a5")
        {
            return $"""
                <!doctype html><html lang="it"><head><meta charset="utf-8"><style>{PrintStyles}</style></head>
                <body class="a5-document"><section class="a5-page">{header}{RenderStages(front)}</section>
                <section class="a5-page">{header}{RenderStages(back)}{RenderUpdates(payload.FieldUpdates)}</section></body></html>
                """;
        }

        return $"""
            <!doctype html><html lang="it"><head><meta charset="utf-8"><style>{PrintStyles}</style></head>
            <body><main class="a4-sheet"><section>{header}{RenderStages(front)}</section>
            <section>{header}{RenderStages(back)}{RenderUpdates(payload.FieldUpdates)}</section></main></body></html>
            """;
    }

    private static string BuildPhoneHtml(
        FreezerCardPayload payload,
        IReadOnlyList<FreezerCardStage> stages,
        bool front)
    {
        var order = payload.DisplayOrder.Count > 0
            ? payload.DisplayOrder
            : stages.OrderBy(x => x.StageNumber).Select(x => x.StageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var startInfo = payload.StartTurn is { } turn && payload.SquadSize is { } squad
            ? $"Turno {turn} / {squad} · stage {payload.StartStage?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}"
            : "Ordine di tiro da confermare";
        return $"""
            <!doctype html><html lang="it"><head><meta charset="utf-8"><style>{PhoneStyles}</style></head>
            <body><main class="phone-card"><header><p class="eyebrow">{(front ? "PIANO CONGELATO" : "CAMPO · AGGIORNAMENTI")}</p>
            <h1>{Encode(payload.MatchName)}</h1><p>{Encode(payload.LocalMatchDate)} · {Encode(payload.Division)} · {Encode(payload.PowerFactor)}</p>
            <p class="turn">{Encode(startInfo)}</p></header><section class="phone-safe-area">
            {RenderStages(stages, order)}{(front ? string.Empty : RenderUpdates(payload.FieldUpdates))}</section></main></body></html>
            """;
    }

    private static string RenderStages(IEnumerable<FreezerCardStage> stages, IReadOnlyList<string>? displayOrder = null)
    {
        var displayIndexes = displayOrder?
            .Select((stage, index) => (stage, index))
            .ToDictionary(item => item.stage, item => item.index, StringComparer.Ordinal);
        var ordered = displayOrder is null
            ? stages.OrderBy(x => x.StageNumber)
            : stages.OrderBy(x =>
            {
                var stageNumber = x.StageNumber.ToString(CultureInfo.InvariantCulture);
                return displayIndexes!.TryGetValue(stageNumber, out var index) ? index : int.MaxValue;
            }).ThenBy(x => x.StageNumber);
        var rows = new StringBuilder("<ol class=\"stages\">");
        foreach (var stage in ordered)
        {
            var center = stage.Center is { } value ? value.ToString("0.00", CultureInfoIt) : "n.d.";
            var low = stage.Lower is { } lower ? lower.ToString("0.00", CultureInfoIt) : "—";
            var high = stage.Upper is { } upper ? upper.ToString("0.00", CultureInfoIt) : "—";
            var strategy = string.IsNullOrWhiteSpace(stage.Strategy) ? string.Empty : $"<p class=\"strategy\">{Encode(stage.Strategy)}</p>";
            rows.Append("<li><strong>").Append(stage.StageNumber.ToString(CultureInfoIt))
                .Append("</strong><span>HF ").Append(center).Append("</span><span class=\"range\">")
                .Append(low).Append("–").Append(high).Append("</span><span class=\"rounds\">")
                .Append(stage.MinimumRounds.ToString(CultureInfoIt)).Append(" colpi</span>")
                .Append(strategy).Append("</li>");
            if (stage.StageNumber == 8)
                rows.Append("<li class=\"lunch\" aria-label=\"Pausa pranzo\">PAUSA PRANZO</li>");
        }
        rows.Append("</ol>");
        return rows.ToString();
    }

    private static string RenderUpdates(IEnumerable<string> updates)
    {
        var items = updates.Take(10).Select(Encode).ToArray();
        return items.Length == 0
            ? string.Empty
            : $"<section class=\"updates\"><h2>Aggiornamenti</h2><ul>{string.Join(string.Empty, items.Select(x => $"<li>{x}</li>"))}</ul></section>";
    }

    private static string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
    private static readonly CultureInfo CultureInfoIt = CultureInfo.GetCultureInfo("it-IT");

    private const string PrintStyles = """
        *{box-sizing:border-box}html,body{margin:0;color:#171c1a;font-family:"Liberation Sans",Arial,sans-serif}
        body{font-size:12pt}.a5-page{width:148mm;height:210mm;padding:9mm;break-after:page;overflow:hidden}
        .a5-page:last-child{break-after:auto}.a5-page header h1,.a4-sheet header h1{font-size:20pt;margin:0 0 2mm}
        header p{margin:0 0 2mm;color:#4d5752}.stages{list-style:none;padding:0;margin:6mm 0}
        .stages li{display:grid;grid-template-columns:10mm 1fr 30mm 19mm;gap:2mm;align-items:baseline;
        min-height:9mm;border-bottom:1px solid #d6ddd8;padding:1.6mm 0;font-size:12pt}
        .stages li strong{font-variant-numeric:tabular-nums}.range,.rounds{font-variant-numeric:tabular-nums;text-align:right}
        .strategy{grid-column:2/5;margin:0;font-size:10pt;line-height:1.2}.lunch{display:block!important;text-align:center;
        letter-spacing:.18em;font-weight:700;border-top:3px double #171c1a!important;border-bottom:3px double #171c1a!important}
        .a4-sheet{width:297mm;height:210mm;display:grid;grid-template-columns:1fr 1fr;overflow:hidden}
        .a4-sheet>section{padding:10mm;border-right:1px dashed #7b8580;overflow:hidden}.updates{margin-top:5mm}
        .updates li{margin:2mm 0}
        """;

    private const string PhoneStyles = """
        *{box-sizing:border-box}html,body{margin:0;width:430px;height:932px;overflow:hidden;background:#f4f2eb;color:#17211e;
        font-family:"Liberation Sans",Arial,sans-serif}.phone-card{width:430px;height:932px;padding:38px 28px}
        header{height:240px;border-bottom:1px solid #aeb8b1}.eyebrow{font-size:12pt;font-weight:700;letter-spacing:.16em;
        color:#a94720;margin:0 0 12px}h1{font-size:24pt;line-height:1.05;margin:0 0 12px;max-height:58px;overflow:hidden}
        header p{font-size:12pt;margin:5px 0}.turn{font-weight:700!important;margin-top:12px!important}
        .phone-safe-area{height:538px;margin-top:0;padding-top:5px;overflow:hidden}.stages{list-style:none;padding:0;margin:0}
        .stages li{display:grid;grid-template-columns:32px 1fr 92px 62px;gap:5px;align-items:center;
        min-height:29px;border-bottom:1px solid #d0d5d0;font-size:12pt;font-variant-numeric:tabular-nums}
        .stages li strong{color:#a94720;font-size:14pt}.range,.rounds{text-align:right;font-size:12pt}
        .strategy{grid-column:2/5;margin:0 0 4px;font-size:12pt;line-height:1.1}
        .lunch{display:block!important;text-align:center;font-size:12pt;letter-spacing:.16em;font-weight:700;
        border-top:3px double #17211e!important;border-bottom:3px double #17211e!important}
        .updates{font-size:12pt;margin-top:8px}.updates h2{font-size:13pt;margin:0}.updates ul{margin:4px 0;padding-left:18px}
        """;
}
