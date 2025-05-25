using MorWalPiz.Contracts.Contracts;
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
        public static ShortLinkContract Convert(ShortLink shortLink,string endpointBasePath)
        {
            return new ShortLinkContract
            {
                Code = shortLink.Code,
                Endpoint = $"{endpointBasePath}/{shortLink.Code}?{shortLink.QueryString}",
                Target = shortLink.Target,
                QueryString = shortLink.QueryString,
                ShortLinkId = shortLink.ShortLinkId,
                ClicksCount = shortLink.ClicksCount,
                LinkType = shortLink.LinkType,
                VideoId = shortLink.VideoId
            };
        }
    }
}
