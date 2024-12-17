using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static async Task CreateVideoShortlink(IMongoCollection<Match> matchCollection, IMongoCollection<ShortLink> shortLinkCollection, HttpClient client, string siteUrl)
        {
            Console.WriteLine("Enter video ID");
            var videoId = Console.ReadLine();
            if (string.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }

            Console.WriteLine("Enter queryString");
            var queryString = Console.ReadLine();


            var existingMatch = matchCollection.Find(x => x.ThumbnailUrl == videoId || x.Videos.Any(v => v.YoutubeId == videoId)).FirstOrDefault();
            if (existingMatch == null)
            {
                Console.WriteLine("Match do not exists");
                return;
            }

            var shortLinkCode = await CalculateShortLink(shortLinkCollection);
            var shortlink = new ShortLink(shortLinkCode,videoId, queryString ?? string.Empty);

            await shortLinkCollection.InsertOneAsync(shortlink);

            var json = await client.GetStringAsync($"reset?k={CacheKeys.ShortLink}");

            Console.WriteLine($"Shortlink: {siteUrl}sl/{shortlink.Code}");
        }

        private static async Task<string> CalculateShortLink(IMongoCollection<ShortLink> shortLinkCollection)
        {
            var shortlinks = (await shortLinkCollection.FindAsync(_ => true)).ToList();
            
            var sl = shortlinks.Select(x => x.Code).ToList();

            return GetUniqueValue(sl);

            string GetUniqueValue(IEnumerable<string> strings)
            {
                // Sort and concatenate the input strings
                string concatenated = string.Join("", strings.OrderBy(s => s));

                // Hash the concatenated string
                using SHA256 sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));

                // Convert hash bytes to a hexadecimal string
                string hash = Convert.ToHexString(hashBytes);

                // Check if the truncated hash conflicts with inputs
                string uniqueString = hash.Substring(0, 5);
                while (strings.Contains(uniqueString))
                {
                    uniqueString = GetUniqueValue([.. strings, uniqueString]);
                }

                return uniqueString;
            }
        }
    }
}
