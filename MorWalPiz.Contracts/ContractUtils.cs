using MorWalPiz.Contracts.Contracts;
using MorWalPiz.Contracts.Contracts.Shop;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorWalPiz.Contracts
{
    public static class ContractUtils
    {

        public static CategoryContract Convert(Category entity)
        {
            return new CategoryContract
            {
                CategoryId = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                ChannelId = entity.ChannelId,
            };
        }
        public static ShortLinkContract Convert(ShortLink entity, string endpointBasePath)
        {
            return new ShortLinkContract
            {
                Code = entity.Code,
                Endpoint = $"{endpointBasePath}/{entity.Code}",
                Target = entity.Target,
                QueryString = entity.QueryString,
                ShortLinkId = entity.Id,
                ClicksCount = entity.ClicksCount,
                LinkType = entity.LinkType,
                ContentId = entity.ContentId,
                ChannelId = entity.ChannelId,
                ManagementChannelId = entity.ManagementChannelId
            };
        }
        public static QueryLinkContract Convert(QueryLink entity)
        {
            return new QueryLinkContract
            {
                QueryLinkId = entity.Id,
                Title = entity.Title,
                Value = entity.Value,
                ChannelId = entity.ChannelId,
            };
        }
        public static UserContract Convert(User entity)
        {
            return new UserContract
            {
                Id = entity.Id,
                Username = entity.Username,
                Email = entity.Email,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Phone = entity.Phone,
                IsActive = entity.IsActive,
                LastLogin = entity.LastLogin
            };
        }
        public static ChannelContract Convert(YTChannel entity)
        {
            return new ChannelContract
            {
                Id = entity.Id,
                ChannelId = entity.ChannelId,
                YTChannelId = entity.ChannelId,
                ChannelName = entity.ChannelName,
                Videos = entity.Videos?.Select(Convert).ToArray() ?? [],
                ShortLinkUrl = entity.ShortLinkUrl,
                IsSHIT = entity.IsSHIT,
                ChannelLogoUrl = entity.ChannelLogoUrl,
                Socials = entity.Socials?.Select(s => new ChannelSocialContract
                {
                    Provider = s.Provider,
                    Handler = s.Handler
                }).ToList() ?? []
            };
        }
        public static ChannelNewsContract Convert(ChannelNews entity)
        {
            return new ChannelNewsContract
            {
                Id = entity.Id,
                ChannelId = entity.ChannelId,
                Title = entity.Title,
                Subtitle = entity.Subtitle,
                DescriptionHtml = entity.DescriptionHtml,
                Images = entity.Images.Select(Convert).ToArray(),
                Slug = entity.Slug,
                Status = entity.Status,
                PublicationTimeUtc = entity.PublicationTimeUtc,
                DisplayOrder = entity.DisplayOrder,
                CreationDateTime = entity.CreationDateTime,
                UpdatedDateTime = entity.UpdatedDateTime
            };
        }
        public static ChannelNewsImageContract Convert(ChannelNewsImage entity) => new()
        {
            StorageKey = entity.StorageKey,
            PublicUrl = entity.PublicUrl,
            ContentType = entity.ContentType,
            Width = entity.Width,
            Height = entity.Height,
            AltText = entity.AltText,
            DisplayOrder = entity.DisplayOrder
        };
        public static ChannelNewsPublicContract ConvertPublic(ChannelNews entity, YTChannel channel, string fallbackLogoUrl)
        {
            return new ChannelNewsPublicContract
            {
                Id = entity.Id,
                Slug = entity.Slug,
                ChannelId = channel.ChannelId,
                ChannelName = channel.ChannelName,
                ChannelLogoUrl = string.IsNullOrWhiteSpace(channel.ChannelLogoUrl) ? fallbackLogoUrl : channel.ChannelLogoUrl,
                Title = entity.Title,
                Subtitle = entity.Subtitle,
                DescriptionHtml = entity.DescriptionHtml,
                Images = entity.Images.Select(image => new ChannelNewsPublicImageContract
                {
                    PublicUrl = image.PublicUrl,
                    ContentType = image.ContentType,
                    Width = image.Width,
                    Height = image.Height,
                    AltText = image.AltText,
                    DisplayOrder = image.DisplayOrder
                }).ToArray(),
                Status = entity.Status,
                PublicationTimeUtc = entity.PublicationTimeUtc
            };
        }
        public static ChannelVideoContract Convert(YouTubeVideo entity)
        {
            return new ChannelVideoContract
            {
                VideoId = entity.VideoId,
                Title = entity.Title,
                LastCommentDate = entity.LastCommentDate
            };
        }
        public static SponsorContract Convert(Sponsor entity)
        {
            return new SponsorContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Url = entity.Url,
                ImgSrc = entity.ImgSrc
            };
        }
        public static SponsorContract Convert(Sponsor entity, string imageBaseUrl)
        {
            return new SponsorContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Url = entity.Url,
                ImgSrc = string.IsNullOrWhiteSpace(entity.ImgSrc) ? entity.ImgSrc : $"{imageBaseUrl}/{entity.ImgSrc}"
            };
        }
        public static CompilationContract Convert(Compilation entity)
        {
            return new CompilationContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                Videos = entity.Videos,
                ChannelId = entity.ChannelId
            };
        }
        public static CalendarEventContract Convert(CalendarEvent entity)
        {
            return new CalendarEventContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Categories = entity.Categories,
                MatchId = entity.MatchId,
                MatchUrl = entity.MatchUrl,
                ChannelId = entity.ChannelId
            };
        }
        public static ConfigurationContract Convert(MorWalPizConfiguration entity)
        {
            return new ConfigurationContract
            {
                Id = entity.Id,
                Key = entity.Key,
                Value = entity.Value,
                Type = entity.Type,
                Description = entity.Description
            };
        }
        public static CustomFormContract Convert(CustomForm entity, int? responseCount = null)
        {
            return new CustomFormContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                Active = entity.Active,
                Questions = entity.Questions,
                ResponseCount = responseCount ?? entity.ResponseCount
            };
        }
        public static InsightTopicContract Convert(InsightTopic entity)
        {
            return new InsightTopicContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                SeedArguments = entity.SeedArguments,
                PreferredSources = entity.PreferredSources,
                ChannelId = entity.ChannelId,
                CreationMode = entity.CreationMode
            };
        }
        public static InsightNewsItemContract Convert(InsightNewsItem entity)
        {
            return new InsightNewsItemContract
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                Title = entity.Title,
                Summary = entity.Summary,
                SourceUrl = entity.SourceUrl,
                SourceName = entity.SourceName,
                Status = entity.Status,
                StarRating = entity.StarRating,
                AIRelevanceScore = entity.AIRelevanceScore,
                DiscoveredAt = entity.DiscoveredAt,
                PlatformSource = entity.PlatformSource,
                PostId = entity.PostId,
                VideoId = entity.EffectiveVideoId,
                AnalysisReason = entity.AnalysisReason,
                ReviewReason = entity.ReviewReason,
                SourceKind = entity.SourceKind,
                CommentExcerpt = entity.CommentExcerpt,
                Sentiment = entity.Sentiment,
                ChannelId = entity.ChannelId,
                SourceComments = entity.SourceComments
            };
        }
        public static InsightContentPlanContract Convert(InsightContentPlan entity)
        {
            return new InsightContentPlanContract
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                Title = entity.Title,
                Type = entity.Type,
                Outline = entity.Outline,
                GeneratedFromNewsItemIds = entity.GeneratedFromNewsItemIds,
                TargetPlatforms = entity.TargetPlatforms,
                GeneratedAt = entity.GeneratedAt,
                ChannelId = entity.ChannelId
            };
        }
        public static PublishScheduleContract Convert(PublishSchedule entity)
        {
            return new PublishScheduleContract
            {
                Id = entity.Id,
                VideoId = entity.VideoId,
                QueryStringIds = entity.QueryStringIds,
                Message = entity.Message,
                Date = entity.Date
            };
        }
        public static SponsorApplyContract Convert(SponsorApply entity)
        {
            return new SponsorApplyContract
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Description = entity.Description
            };
        }
        public static UserRequestContract Convert(UserRequest entity)
        {
            return new UserRequestContract
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Topic = entity.Topic,
                Description = entity.Description,
                Status = entity.Status,
                AdminNote = entity.AdminNote,
                Votes = entity.Votes
            };
        }
        public static YouTubeContentContract Convert(YouTubeContent entity)
        {
            return new YouTubeContentContract
            {
                Id = entity.Id,
                ContentId = entity.ContentId,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                ThumbnailVideoId = entity.ThumbnailVideoId,
                VideoRefs = entity.VideoRefs,
                Categories = entity.Categories,
                ContentType = entity.ContentType,
                YouTubeVideoLinks = entity.YouTubeVideoLinks,
                ShortLinks = entity.ShortLinks,
                IsPrivate = entity.IsPrivate,
                CreatorUserId = entity.CreatorUserId,
                OwnerChannelId = entity.OwnerChannelId
            };
        }
        public static ProductContract Convert(Product entity)
        {
            return new ProductContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                Categories = entity.Categories,
                CreationDateTime = entity.CreationDateTime
            };
        }
        public static ProductCategoryContract Convert(ProductCategory entity)
        {
            return new ProductCategoryContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                CreationDateTime = entity.CreationDateTime
            };
        }
        public static QuickLinksContract Convert(QuickLinks entity)
        {
            return new QuickLinksContract
            {
                Id = entity.Id,
                Title = entity.Title,
                Subtitle = entity.Subtitle,
                Url = entity.Url,
                ChannelId = entity.ChannelId,
                Links = entity.Links.Select(Convert).ToArray(),
                CreationDateTime = entity.CreationDateTime
            };
        }
        public static QuickLinksPublicContract ConvertPublic(QuickLinks entity)
        {
            return new QuickLinksPublicContract
            {
                Title = entity.Title,
                Subtitle = entity.Subtitle,
                Url = entity.Url,
                Links = entity.Links.Select(Convert).ToArray()
            };
        }
        private static QuickLinkContract Convert(QuickLink link)
        {
            return new QuickLinkContract
            {
                Kind = link.Kind,
                TargetUrl = link.TargetUrl,
                Title = link.Title,
                Subtitle = link.Subtitle,
                Label = link.Label,
                ImageUrl = link.ImageUrl,
                Icon = link.Icon,
                Provider = link.Provider
            };
        }
        public static DigitalProductContract Convert(DigitalProduct entity)
        {
            return new DigitalProductContract
            {
                DigitalProductId = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                PreviewImageUrl = entity.PreviewImageUrl,
                CategoryIds = entity.CategoryIds,
                Price = entity.Price,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static DigitalProductCategoryContract Convert(DigitalProductCategory entity)
        {
            return new DigitalProductCategoryContract
            {
                DigitalProductCategoryId = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                DisplayOrder = entity.DisplayOrder
            };
        }
    }
}
