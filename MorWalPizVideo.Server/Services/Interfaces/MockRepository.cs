﻿using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public class MatchMockRepository : BaseMockRepository<Match>, IMatchRepository
    {
        public MatchMockRepository(IWebHostEnvironment environment) : base(environment, "matches")
        {
        }
    }
    public class PageMockRepository : BaseMockRepository<Page>, IPageRepository
    {
        public PageMockRepository(IWebHostEnvironment environment) : base(environment, "pages")
        {
        }
    }
    public class ProductMockRepository : BaseMockRepository<Product>, IProductRepository
    {
        public ProductMockRepository(IWebHostEnvironment environment) : base(environment, "products")
        {
        }
    }
    public class SponsorMockRepository : BaseMockRepository<Sponsor>, ISponsorRepository
    {
        public SponsorMockRepository(IWebHostEnvironment environment) : base(environment, "sponsors")
        {
        }
    }
    public class CalendarEventMockRepository : BaseMockRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventMockRepository(IWebHostEnvironment environment) : base(environment, "calendarEvents")
        {
        }
    }
}
