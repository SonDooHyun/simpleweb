using WebServer.Helpers;
using WebServer.Log;
using WebServer.Models;

namespace WebServer.Services
{
    public partial class EmployeeService
    {
        public async Task<GetPagedEmplyeeResponse> GetPagedEmployeesAsync(int page, int pageSize)
        {
            GetPagedEmplyeeResponse response = new();

            try
            {
                int totalCount = await this.context.GetTotalCountAsync();
                if (totalCount == 0)
                {
                    throw new AppException(EErrorCode.ERR_NotExist_Employee,
                        $"employee count zero...");
                }

                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                response.TotalCount = totalCount;
                response.TotalPages = totalPages;
                response.CurrentPage = page;
                response.PageSize = pageSize;
                response.Employees = await this.context.GetPagedEmployeesAsync(page, pageSize);

            }
            catch (AppException appEx)
            {
                string errorString = appEx.ErrorCode.GetErrorString();
                string message = string.IsNullOrEmpty(appEx.Message) ? errorString : errorString + appEx.Message;
                
                response.ErrorCode = (int)appEx.ErrorCode;
                response.ErrorDescription = errorString;

                AppLog.Error(message, appEx);
            }
            catch (Exception ex)
            {
                response.ErrorCode = (int)EErrorCode.Unknown;
                response.ErrorDescription = EErrorCode.Unknown.GetErrorString();
                AppLog.Fatal(ex.Message, ex);
            }

            return response;
        }

        public async Task<GetEmployeeResponse> GetEmployeeByNameAsync(string name)
        {
            GetEmployeeResponse response = new();

            try
            {
                if (await this.context.GetTotalCountAsync() == 0)
                {
                    throw new AppException(EErrorCode.ERR_NotExist_Employee,
                        $"employee count zero...");
                }

                response.Employees = await this.context.GetEmployeesByNameAsync(name);
                if (response.Employees == null || response.Employees.Count() == 0)
                {
                    throw new AppException(EErrorCode.ERR_NotExist_Employee, 
                        $"request name : {name}");
                }
            }
            catch (AppException appEx)
            {
                string errorString = appEx.ErrorCode.GetErrorString();
                string message = string.IsNullOrEmpty(appEx.Message) ? errorString : errorString + appEx.Message;
                
                response.ErrorCode = (int)appEx.ErrorCode;
                response.ErrorDescription = errorString;

                AppLog.Error(message, appEx);
            }
            catch (Exception ex)
            {
                response.ErrorCode = (int)EErrorCode.Unknown;
                response.ErrorDescription = EErrorCode.Unknown.GetErrorString();
                AppLog.Fatal(ex.Message, ex);
            }

            return response;
        }
    }
}
