using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Models;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Services
{
    public class InsightAgentService : IInsightAgentService
    {
        private readonly Kernel _kernel;

        public InsightAgentService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<IList<InsightNewsItem>> DiscoverNewsAsync(InsightTopic topic)
        {
            var seedArgumentsString = string.Join(", ", topic.SeedArguments);
            var preferredSourcesString = topic.PreferredSources.Any() 
                ? $"Focus on these sources: {string.Join(", ", topic.PreferredSources)}" 
                : "Search across all available sources";

            var prompt = @$"
You are a news discovery agent specializing in finding relevant and high-quality news articles.

Topic: {topic.Title}
Description: {topic.Description}
Seed Arguments: {seedArgumentsString}
{preferredSourcesString}

Your task is to discover recent news articles (from the last 30 days) that are highly relevant to this topic.

For each news item, provide:
- Title: The article headline
- Summary: A brief 2-3 sentence summary highlighting key points
- SourceUrl: The URL to the article
- SourceName: The name of the publication or website
- AIRelevanceScore: Your assessment of relevance (0.0 to 1.0, where 1.0 is most relevant)
- DiscoveredAt: Current timestamp in ISO 8601 format

Discovery Guidelines:
1. Prioritize authoritative sources and original reporting
2. Look for recent developments, trends, and insights related to the seed arguments
3. Avoid duplicate or similar stories
4. Focus on substantive content rather than clickbait
5. Ensure diversity of perspectives and sources
6. Aim to discover 5-10 high-quality items

Return results as a JSON array following the specified schema.";

            var trimmedPrompt = PrettifyString(prompt);

#pragma warning disable SKEXP0010
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(NewsDiscoveryResponse)
            };
#pragma warning restore SKEXP0010

            var result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));
            var discoveryResponse = JsonSerializer.Deserialize<NewsDiscoveryResponse>(result.ToString()) 
                ?? new NewsDiscoveryResponse();

            // Convert to domain models
            var newsItems = discoveryResponse.NewsItems.Select(item => new InsightNewsItem(
                topicId: topic.Id,
                title: item.Title,
                summary: item.Summary,
                sourceUrl: item.SourceUrl,
                sourceName: item.SourceName,
                status: InsightNewsStatus.Pending,
                starRating: 0,
                aiRelevanceScore: item.AIRelevanceScore,
                discoveredAt: DateTime.Parse(item.DiscoveredAt)
            )
            {
                Id = Guid.NewGuid().ToString()
            }).ToList();

            return newsItems;
        }

        public async Task<IList<InsightNewsItem>> RankNewsItemsAsync(IList<InsightNewsItem> newsItems)
        {
            // Calculate ranking scores for all items
            var rankedItems = newsItems
                .Select(item => new
                {
                    Item = item,
                    RankingScore = item.CalculateRankingScore()
                })
                .OrderByDescending(x => x.RankingScore)
                .Select(x => x.Item)
                .ToList();

            // If there are many items, use AI to refine the ranking
            if (newsItems.Count > 10)
            {
                var itemSummaries = string.Join("\n", rankedItems.Take(20).Select((item, index) => 
                    $"{index + 1}. {item.Title} (Score: {item.CalculateRankingScore():F2}, AI: {item.AIRelevanceScore:F2}, Stars: {item.StarRating})"));

                var prompt = @$"
You are a content curation expert. Review this ranked list of news items and provide refinement suggestions.

Current Rankings:
{itemSummaries}

Consider:
1. Are the most relevant items at the top?
2. Is there good diversity in the top results?
3. Are any lower-ranked items undervalued?

Provide a refined ranking order (list of item numbers in order) and brief justification.
Return as JSON with 'refinedOrder' array and 'justification' string.";

                var trimmedPrompt = PrettifyString(prompt);

#pragma warning disable SKEXP0010
                var executionSettings = new AzureOpenAIPromptExecutionSettings()
                {
                    ResponseFormat = typeof(RankingRefinementResponse)
                };
#pragma warning restore SKEXP0010

                try
                {
                    var result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));
                    var refinement = JsonSerializer.Deserialize<RankingRefinementResponse>(result.ToString());

                    if (refinement?.RefinedOrder != null && refinement.RefinedOrder.Any())
                    {
                        // Apply AI refinement to the ranking
                        var refinedList = new List<InsightNewsItem>();
                        foreach (var index in refinement.RefinedOrder)
                        {
                            if (index > 0 && index <= rankedItems.Count)
                            {
                                refinedList.Add(rankedItems[index - 1]);
                            }
                        }

                        // Add any items not in the refined order
                        foreach (var item in rankedItems)
                        {
                            if (!refinedList.Contains(item))
                            {
                                refinedList.Add(item);
                            }
                        }

                        return refinedList;
                    }
                }
                catch
                {
                    // If AI refinement fails, fall back to algorithmic ranking
                }
            }

            return rankedItems;
        }

        public async Task<InsightContentPlan> GenerateContentPlanAsync(
            string topicId,
            IList<string> newsItemIds,
            ContentPlanType contentType,
            IList<string> targetPlatforms)
        {
            // This method would need access to the news items to generate the plan
            // For now, we'll generate a placeholder structure
            var contentTypeDescriptions = new Dictionary<ContentPlanType, string>
            {
                { ContentPlanType.Article, "a comprehensive article" },
                { ContentPlanType.Podcast, "a podcast episode script" },
                { ContentPlanType.SocialPost, "social media content" },
                { ContentPlanType.VideoScript, "a video script" },
                { ContentPlanType.Newsletter, "a newsletter edition" }
            };

            var typeDesc = contentTypeDescriptions.GetValueOrDefault(contentType, "content");
            var platformsString = string.Join(", ", targetPlatforms);

            var prompt = @$"
You are a content strategist creating {typeDesc} based on recent news and insights.

Content Type: {contentType}
Target Platforms: {platformsString}
Number of Source Articles: {newsItemIds.Count}

Generate a detailed content plan that includes:
1. A compelling title
2. A structured outline with main sections/segments
3. Key talking points and angles
4. Suggested hooks and engagement strategies
5. Platform-specific adaptation notes

The plan should synthesize multiple news sources into a cohesive narrative that provides value to the audience.

Return as JSON with 'title' and 'outline' (detailed markdown-formatted outline).";

            var trimmedPrompt = PrettifyString(prompt);

#pragma warning disable SKEXP0010
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(ContentPlanResponse)
            };
#pragma warning restore SKEXP0010

            var result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));
            var planResponse = JsonSerializer.Deserialize<ContentPlanResponse>(result.ToString()) 
                ?? new ContentPlanResponse { Title = "Generated Content Plan", Outline = "Content outline..." };

            var contentPlan = new InsightContentPlan(
                topicId: topicId,
                title: planResponse.Title,
                type: contentType,
                outline: planResponse.Outline,
                generatedFromNewsItemIds: newsItemIds.ToArray(),
                targetPlatforms: targetPlatforms.ToArray(),
                generatedAt: DateTime.UtcNow
            )
            {
                Id = Guid.NewGuid().ToString()
            };

            return contentPlan;
        }

        private string PrettifyString(string s)
        {
            string[] rows = s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = rows[i].TrimStart();
            }
            return string.Join(Environment.NewLine, rows);
        }

        // DTOs for AI responses
        private class NewsDiscoveryResponse
        {
            public List<NewsItemDto> NewsItems { get; set; } = new();
        }

        private class NewsItemDto
        {
            public string Title { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public string SourceUrl { get; set; } = string.Empty;
            public string SourceName { get; set; } = string.Empty;
            public double AIRelevanceScore { get; set; }
            public string DiscoveredAt { get; set; } = string.Empty;
        }

        private class RankingRefinementResponse
        {
            public List<int> RefinedOrder { get; set; } = new();
            public string Justification { get; set; } = string.Empty;
        }

        private class ContentPlanResponse
        {
            public string Title { get; set; } = string.Empty;
            public string Outline { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Mock implementation of IInsightAgentService for testing and development.
    /// Provides deterministic, synthetic data without external AI dependencies.
    /// </summary>
    public class MockInsightAgentService : IInsightAgentService
    {
        private static readonly string[] MockSourceNames = new[]
        {
            "Tech News Daily",
            "Industry Insights",
            "Global Business Review",
            "Digital Trends",
            "Innovation Today",
            "Market Watch",
            "Research Quarterly"
        };

        private static readonly string[] MockTitlePrefixes = new[]
        {
            "Breaking:",
            "Analysis:",
            "Study Reveals:",
            "Expert Opinion:",
            "Emerging Trend:",
            "Industry Report:",
            "New Research:"
        };

        public Task<IList<InsightNewsItem>> DiscoverNewsAsync(InsightTopic topic)
        {
            var newsItems = new List<InsightNewsItem>();
            var random = new Random(topic.Id.GetHashCode()); // Deterministic based on topic ID
            var itemCount = random.Next(5, 11); // 5-10 items

            for (int i = 0; i < itemCount; i++)
            {
                var sourceIndex = random.Next(MockSourceNames.Length);
                var prefixIndex = random.Next(MockTitlePrefixes.Length);
                var seedArg = topic.SeedArguments.Any() 
                    ? topic.SeedArguments[random.Next(topic.SeedArguments.Count())] 
                    : topic.Title;

                var newsItem = new InsightNewsItem(
                    topicId: topic.Id,
                    title: $"{MockTitlePrefixes[prefixIndex]} {seedArg} - Development #{i + 1}",
                    summary: $"This is a synthetic news item about {topic.Title}. " +
                             $"It relates to {seedArg} and provides insights into current trends. " +
                             $"This mock data is generated for testing purposes without AI dependencies.",
                    sourceUrl: $"https://example.com/news/{topic.Id}/{i + 1}",
                    sourceName: MockSourceNames[sourceIndex],
                    status: InsightNewsStatus.Pending,
                    starRating: 0,
                    aiRelevanceScore: Math.Round(0.6 + (random.NextDouble() * 0.4), 2), // 0.6-1.0
                    discoveredAt: DateTime.UtcNow.AddDays(-random.Next(1, 30)) // Last 30 days
                )
                {
                    Id = Guid.NewGuid().ToString()
                };

                newsItems.Add(newsItem);
            }

            return Task.FromResult<IList<InsightNewsItem>>(newsItems);
        }

        public Task<IList<InsightNewsItem>> RankNewsItemsAsync(IList<InsightNewsItem> newsItems)
        {
            // Use only the algorithmic ranking (no AI refinement in mock)
            var rankedItems = newsItems
                .Select(item => new
                {
                    Item = item,
                    RankingScore = item.CalculateRankingScore()
                })
                .OrderByDescending(x => x.RankingScore)
                .Select(x => x.Item)
                .ToList();

            return Task.FromResult<IList<InsightNewsItem>>(rankedItems);
        }

        public Task<InsightContentPlan> GenerateContentPlanAsync(
            string topicId,
            IList<string> newsItemIds,
            ContentPlanType contentType,
            IList<string> targetPlatforms)
        {
            var contentTypeNames = new Dictionary<ContentPlanType, string>
            {
                { ContentPlanType.Article, "Article" },
                { ContentPlanType.Podcast, "Podcast Episode" },
                { ContentPlanType.SocialPost, "Social Media Post" },
                { ContentPlanType.VideoScript, "Video Script" },
                { ContentPlanType.Newsletter, "Newsletter" }
            };

            var typeName = contentTypeNames.GetValueOrDefault(contentType, "Content");
            var platformsString = string.Join(", ", targetPlatforms);

            var outline = $@"# {typeName} Content Plan

## Overview
This is a mock content plan generated for testing purposes, based on {newsItemIds.Count} news sources.

## Target Platforms
{platformsString}

## Main Sections

### 1. Introduction
- Hook: Engaging opening statement
- Context: Brief background on the topic
- Preview: What the audience will learn

### 2. Key Insights
- Insight #1: Primary finding from news analysis
- Insight #2: Secondary trend or development
- Insight #3: Expert perspective or data point

### 3. Deep Dive
- Detailed exploration of main themes
- Supporting evidence and examples
- Connections between different sources

### 4. Implications
- What this means for the audience
- Actionable takeaways
- Future considerations

### 5. Conclusion
- Summary of key points
- Call to action
- Next steps or resources

## Engagement Strategies
- Use compelling visuals or quotes
- Include interactive elements where appropriate
- Encourage audience discussion
- Share across multiple platforms for maximum reach

## Notes
This is a mock outline. In production, this would be generated by AI based on actual news content.";

            var contentPlan = new InsightContentPlan(
                topicId: topicId,
                title: $"Mock {typeName}: Comprehensive Analysis",
                type: contentType,
                outline: outline,
                generatedFromNewsItemIds: newsItemIds.ToArray(),
                targetPlatforms: targetPlatforms.ToArray(),
                generatedAt: DateTime.UtcNow
            )
            {
                Id = Guid.NewGuid().ToString()
            };

            return Task.FromResult(contentPlan);
        }
    }
}
