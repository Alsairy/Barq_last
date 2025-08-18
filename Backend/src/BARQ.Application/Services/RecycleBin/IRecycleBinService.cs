namespace BARQ.Application.Services.RecycleBin
{
    public interface IRecycleBinService
    {
        Task<object> ListDeletedAsync(string entity, int page, int pageSize);
        Task<bool> RestoreAsync(string entity, Guid id);
    }
}
