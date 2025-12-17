using RestAPI.Helper;
using RestAPI.Models;

namespace RestAPI.Interface
{
    public interface IEmpRepository
    {

        Task<List<Emp>> GetAllAsync();
        Task<Emp?> GetByIdAsync(int id);
        Task AddAsync(Emp emp);
        Task<bool> EmailExistsAsync(string email, int? ignoreId = null);
        Task SaveAsync();
        void Remove(Emp emp);
        Task<List<Emp>> GetAllAsync(QueryObject query);

    }
}
