using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync();
    Task<IReadOnlyList<VideoPublicationDayResponse>> GetVideoPublicationsAsync(int days);
}