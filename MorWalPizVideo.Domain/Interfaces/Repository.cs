using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class YouTubeContentRepository : BaseRepository<YouTubeContent>, IYouTubeContentRepository
    {
        public YouTubeContentRepository(IMongoDatabase database) : base(database, DbCollections.YouTubeContent)
        {
        }
    }
    public class QueryLinkRepository : BaseRepository<QueryLink>, IQueryLinkRepository
    {
        public QueryLinkRepository(IMongoDatabase database) : base(database, DbCollections.QueryLinks)
        {
        }
    }
    public class PublishScheduleRepository : BaseRepository<PublishSchedule>, IPublishScheduleRepository
    {
        public PublishScheduleRepository(IMongoDatabase database) : base(database, DbCollections.PublishSchedules)
        {
        }
    }
    public class PageRepository : BaseRepository<Page>, IPageRepository
    {
        public PageRepository(IMongoDatabase database) : base(database, DbCollections.Pages)
        {
        }
    }
    public class SponsorRepository : BaseRepository<Sponsor>, ISponsorRepository
    {
        public SponsorRepository(IMongoDatabase database) : base(database, DbCollections.Sponsors)
        {
        }
    }
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(IMongoDatabase database) : base(database, DbCollections.Products)
        {
        }
    }
    public class ProductCategoryRepository : BaseRepository<ProductCategory>, IProductCategoryRepository
    {
        public ProductCategoryRepository(IMongoDatabase database) : base(database, DbCollections.ProductCategories)
        {
        }
    }

    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(IMongoDatabase database) : base(database, DbCollections.CalendarEvents)
        {
        }
    }

    public class CompilationRepository : BaseRepository<Compilation>, ICompilationRepository
    {
        public CompilationRepository(IMongoDatabase database) : base(database, DbCollections.Compilations)
        {
        }
    }

    public class ShortLinkRepository : BaseRepository<ShortLink>, IShortLinkRepository
    {
        public ShortLinkRepository(IMongoDatabase database) : base(database, DbCollections.ShortLinks)
        {
        }
    }

    public class ConfigurationRepository : BaseRepository<MorWalPizConfiguration>, IConfigurationRepository
    {
        public ConfigurationRepository(IMongoDatabase database) : base(database, DbCollections.Configurations)
        {
        }
    }
    public class YTChannelRepository : BaseRepository<YTChannel>, IYTChannelRepository
    {
        public YTChannelRepository(IMongoDatabase database) : base(database, DbCollections.Channels)
        {
        }
    }

    public class SponsorApplyRepository : BaseRepository<SponsorApply>, ISponsorApplyRepository
    {
        public SponsorApplyRepository(IMongoDatabase database) : base(database, DbCollections.SponsorApplies)
        {
        }
    }
    public class BioLinkRepository : BaseRepository<BioLink>, IBioLinkRepository
    {
        public BioLinkRepository(IMongoDatabase database) : base(database, DbCollections.BioLinks)
        {
        }
    }
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(IMongoDatabase database) : base(database, DbCollections.Categories)
        {
        }
    }

    public class CustomFormRepository : BaseRepository<CustomForm>, ICustomFormRepository
    {
        public CustomFormRepository(IMongoDatabase database) : base(database, DbCollections.CustomForms)
        {
        }
    }

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IMongoDatabase database) : base(database, DbCollections.Users)
        {
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await FindByUsernameAsync(username) ?? await FindByEmailAsync(username);
            
            if (user == null)
                return null;

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash, user.Salt))
                return null;

            return user;
        }

        private static bool VerifyPassword(string password, string hash, string salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000);
            var testHash = Convert.ToBase64String(pbkdf2.GetBytes(256));
            return testHash == hash;
        }

        public static string HashPassword(string password, out string salt)
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
            return Convert.ToBase64String(pbkdf2.GetBytes(256));
        }
    }

    public class LoginAttemptRepository : BaseRepository<LoginAttempt>, ILoginAttemptRepository
    {
        public LoginAttemptRepository(IMongoDatabase database) : base(database, DbCollections.LoginAttempts)
        {
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        public async Task<List<LoginAttempt>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        public async Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );
            
            return (int)await _collection.CountDocumentsAsync(filter);
        }

        public async Task<int> GetFailedAttemptsCountByUsernameAsync(string username, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false),
                Builders<LoginAttempt>.Filter.Gte(a => a.AttemptTime, cutoffTime)
            );
            
            return (int)await _collection.CountDocumentsAsync(filter);
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByIpAsync(string ipAddress)
        {
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.IpAddress, ipAddress),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false)
            );
            
            var lastAttempt = await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .FirstOrDefaultAsync();
                
            return lastAttempt?.AttemptTime;
        }

        public async Task<DateTime?> GetLastFailedAttemptTimeByUsernameAsync(string username)
        {
            var filter = Builders<LoginAttempt>.Filter.And(
                Builders<LoginAttempt>.Filter.Eq(a => a.Username, username),
                Builders<LoginAttempt>.Filter.Eq(a => a.IsSuccessful, false)
            );
            
            var lastAttempt = await _collection.Find(filter)
                .SortByDescending(a => a.AttemptTime)
                .FirstOrDefaultAsync();
                
            return lastAttempt?.AttemptTime;
        }

        public async Task CleanupOldAttemptsAsync(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
            var filter = Builders<LoginAttempt>.Filter.Lt(a => a.AttemptTime, cutoffTime);
            
            await _collection.DeleteManyAsync(filter);
        }
    }
}
