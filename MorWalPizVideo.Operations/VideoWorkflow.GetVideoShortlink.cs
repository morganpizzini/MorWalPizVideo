using Microsoft.VisualBasic;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.Operations
{
    static partial class VideoWorkflow
    {
        public static async Task GetVideoShortlink(IMongoCollection<ShortLink> shortLinkCollection, string siteUrl)
        {
            Console.WriteLine("Enter video ID");
            var videoId = Console.ReadLine();
            if (string.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Not a valid ID");
                return;
            }

            var shortlinks = (await shortLinkCollection.FindAsync(x => x.VideoId == videoId)).ToList();

            if(shortlinks.Count == 0)
            {
                Console.WriteLine("No shortlink found for this video");
                return;
            }

            Console.WriteLine("Shortlink list:");
            foreach (var item in shortlinks)
            {
                Console.WriteLine($"{siteUrl}sl/{item.Code}   QueryString: {item.QueryString}");
            }
        }
    }
}
