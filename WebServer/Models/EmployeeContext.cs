using WebServer.Helpers;
using Microsoft.EntityFrameworkCore;

namespace WebServer.Models
{
    public class EmployeeContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("EmployeeDbConnection");
            }
        }

        public override async void Dispose()
        {
            this.Employees.RemoveRange(this.Employees.ToList());
            await SaveChangesAsync();
        }

        public async Task<int> AddEmployeesAsync(IEnumerable<Employee> employees)
        {
            this.Employees.AddRange(employees);
            return await SaveChangesAsync();
        }

        public async Task<bool> IsExistEmailAsync(string email)
        {
            return await this.Employees.AnyAsync(e => e.email == email);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await this.Employees.CountAsync();
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByNameAsync(string name)
        {
            return await this.Employees.Where(e => e.name == name).ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetPagedEmployeesAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
            {
                throw new AppException(EErrorCode.ERR_Invalid_Page_OR_PageSize,
                    $"request page : {page}, pageSize : {pageSize}");
            }

            int skip = (page - 1) * pageSize;
            return await this.Employees.Skip(skip).Take(pageSize).ToListAsync();
        }
    }
}
