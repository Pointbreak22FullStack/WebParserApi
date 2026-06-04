namespace Application.Common.Interfaces
{
    public interface IAiService
    {
        Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
        Task<string> CompleteWithImageAsync(byte[] imageBytes, string prompt, CancellationToken ct = default);
    }
}
