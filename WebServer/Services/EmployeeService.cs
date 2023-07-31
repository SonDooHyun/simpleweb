using WebServer.Helpers;
using WebServer.Log;
using WebServer.Models;
using Newtonsoft.Json;

namespace WebServer.Services
{
    public partial class EmployeeService : IEmployeeService
    {
        private EmployeeContext context;

        public const string CSV = ".csv";
        public const string JSON = ".json";
        public const int EmployeeMemberCnt = 4;

        public EmployeeService(EmployeeContext context) 
        {
            this.context = context;
        }

        public async Task<RegistMemberResponse> RegistEmployee(RegistMemberRequest request)
        {
            RegistMemberResponse response = new();
            List<Employee> employeesList = new();

            try 
            {
                if (request.files != null)
                {
                    await foreach (var employee in GetEmployeeFromFile(request.files))
                    {
                        employeesList.Add(employee);
                    }
                }

                if (request.memberinfos != null)
                {
                    await foreach (var employee in GetEmployeeFromString(request.memberinfos))
                    {
                        employeesList.Add(employee);
                    }
                }

                foreach (var employee in employeesList)
                {
                    if (await this.context.IsExistEmailAsync(employee.email))
                    {
                        throw new AppException(EErrorCode.ERR_Already_Exist_Email, 
                            $"duplicated email {employee.email}");
                    }

                    employee.Validate();
                }

                await this.context.AddEmployeesAsync(employeesList);

                response.Employees = employeesList;
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

        public async IAsyncEnumerable<Employee> GetEmployeeFromString(string memberinfos)
        {
            var employees = DetermineFormat(memberinfos) switch
            {
                CSV => ReadCsvString(memberinfos),
                JSON => ReadJsonString(memberinfos),
                _ => null
            };

            if (employees != null && employees.Any())
            {
                foreach (var employee in employees)
                {
                    yield return employee;
                }
            }
        }

        private string DetermineFormat(string input)
        {
            input = input.Trim();

            if (((input.StartsWith("[") && input.EndsWith("]")) ||
                (input.StartsWith("{") && input.EndsWith("}"))) &&
                input.Contains("{") && input.Contains("}"))
            {
                return JSON;
            }
            else if (input.Contains(","))
            {
                return CSV;
            }
            
            throw new AppException(EErrorCode.ERR_Unknown_File_Format);
        }

        public async IAsyncEnumerable<Employee> GetEmployeeFromFile(IFormFileCollection files)
        {
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var employees = Path.GetExtension(file.FileName) switch
                    {
                        CSV => await ReadCsvFileAsync(file),
                        JSON => await ReadJsonFileAsync(file),
                        _ => null
                    };

                    if (employees != null && employees.Any())
                    {
                        foreach (var employee in employees)
                        {
                            yield return employee;
                        }
                    }
                }
            }
        }

        public async Task<List<Employee>> ReadJsonFileAsync(IFormFile file)
        {
            List<Employee> result = new();
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var employees = JsonConvert.DeserializeObject<List<Employee>>(content);
            if (employees != null && employees.Count > 0)
            {
                return employees;
            }

            return result;
        }

        public async Task<List<Employee>> ReadCsvFileAsync(IFormFile file)
        {
            List<Employee> result = new();

            using var reader = new StreamReader(file.OpenReadStream());
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                var values = line.Split(',');

                var employee = new Employee
                {
                    name = values[0].Trim(),
                    email = values[1].Trim(),
                    tel = values[2].Trim(),
                    joined = DateTime.Parse(values[3].Trim())
                };

                result.Add(employee);
            }

            return result;
        }

        public List<Employee> ReadJsonString(string json)
        {
            List<Employee> result = new();

            var employees = JsonConvert.DeserializeObject<List<Employee>>(json);
            if (employees != null && employees.Count > 0)
            {
                result.AddRange(employees);
            }

            return result;
        }

        public List<Employee> ReadCsvString(string csv)
        {
            var employeesList = new List<Employee>();

            var lines = csv.Split('\n');
            foreach (var line in lines)
            {
                var values = line.Trim().Split(',');

                if (values.Length != EmployeeMemberCnt)
                    throw new AppException(EErrorCode.ERR_Invalid_Data_Format, csv);

                employeesList.Add(new Employee
                {
                    name = values[0].Trim(),
                    email = values[1].Trim(),
                    tel = values[2].Trim(),
                    joined = DateTime.Parse(values[3].Trim())
                });
            }

            return employeesList;
        }
    }
}
