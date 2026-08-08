using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(string channelId);
    Task<IReadOnlyList<VideoPublicationDayResponse>> GetVideoPublicationsAsync(int days, string channelId);
}