using WebServer.Models;

namespace WebServer.Services
{
    public interface IEmployeeService
    {
        public Task<GetPagedEmplyeeResponse> GetPagedEmployeesAsync(int page, int pageSize);
        public Task<GetEmployeeResponse> GetEmployeeByNameAsync(string name);
        public Task<RegistMemberResponse> RegistEmployee(RegistMemberRequest request);
    }
}
