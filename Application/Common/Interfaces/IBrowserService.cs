namespace Application.Common.Interfaces
{
    public interface IBrowserService
    {
        Task OpenPageAsync(string url, CancellationToken ct = default);
    }
}
