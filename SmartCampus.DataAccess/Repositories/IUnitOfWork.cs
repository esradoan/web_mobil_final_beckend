using System;
using System.Threading.Tasks;
using SmartCampus.Entities;

namespace SmartCampus.DataAccess.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : BaseEntity;
        Task<int> CompleteAsync();
    }
}
