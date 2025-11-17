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

        public static CategoryContract Convert(Category entity)
        {
            return new CategoryContract
            {
                CategoryId = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
            };
        }
        public static ShortLinkContract Convert(ShortLink entity, string endpointBasePath)
        {
            return new ShortLinkContract
            {
                Code = entity.Code,
                Endpoint = $"{endpointBasePath}/{entity.Code}?{entity.QueryString}",
                Target = entity.Target,
                QueryString = entity.QueryString,
                ShortLinkId = entity.Id,
                ClicksCount = entity.ClicksCount,
                LinkType = entity.LinkType,
                VideoId = entity.VideoId
            };
        }
        public static QueryLinkContract Convert (QueryLink entity)
        {
            return new QueryLinkContract
            {
                QueryLinkId = entity.Id,
                Title =  entity.Title,
                Value = entity.Value,
            };
        }
    }
}
