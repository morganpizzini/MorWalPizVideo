namespace MorWalPizVideo.BackOffice.DTOs;

public class FontCategoryResponse
{
    public string CategoryName { get; set; } = string.Empty;
    public List<string> FontFiles { get; set; } = new();
}

public class FontListResponse
{
    public List<FontCategoryResponse> Categories { get; set; } = new();
}
