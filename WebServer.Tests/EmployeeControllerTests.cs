using WebServer.Controllers;
using WebServer.Models;
using WebServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace WebServer.Tests
{
    [TestFixture]
    public class EmployeeControllerTests
    {
        private const string TestDataJsonPath = "./TestData/employees.json";
        private const string TestDataJsonFailPath = "./TestData/employees_fail.json";
        private const string TestDataJsonAddPath = "./TestData/employees_add.json";
        private const string TestDataCsvPath = "./TestData/employees.csv";
        private const string TestDataCsvFailPath = "./TestData/employees_fail.csv";

        private const int TestDataJsonFileAddLineCount = 3;
        private const int TestDataCsvFileLineCount = 22;

        private EmployeeContext dbContext;
        private EmployeeService service;
        private EmployeeController controller;
        private List<Employee> employees;

        [SetUp]
        public void Setup()
        {
            this.employees = GetSampleEmployees();

            this.dbContext = new EmployeeContext();
            this.dbContext.Employees.AddRange(this.employees);
            this.dbContext.SaveChanges();

            this.service = new EmployeeService(this.dbContext);
            this.controller = new EmployeeController(this.service);
        }

        [TearDown]
        public void TearDown()
        {
            this.dbContext.Dispose();
            this.employees.Clear();
        }

        // 값이 다른 테스트에서 추가되면 예상치 못한 결과가 나오므로 먼저 테스트 실행
        [Test, Order(0)]
        public async Task Get_ReturnsOkResultWithPaging()
        {
            int page = 2;
            int pageSize = 10;

            var actionResult = await this.controller.GetPagedEmplyeeList(page, pageSize);
            var okResult = actionResult.Result as OkObjectResult;

            Assert.NotNull(okResult);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.NotNull(okResult.Value);

            // 데이터 값 검증
            int totalPages = (int)Math.Ceiling((double)this.employees.Count / pageSize);

            var response = okResult.Value as GetPagedEmplyeeResponse;
            Assert.NotNull(response);

            Assert.That(response.TotalCount, Is.EqualTo(this.employees.Count));
            Assert.That(response.TotalPages, Is.EqualTo(totalPages));
            Assert.That(response.CurrentPage, Is.EqualTo(page));
            Assert.That(response.PageSize, Is.EqualTo(pageSize));
            Assert.That(response.Employees.Count(), Is.LessThanOrEqualTo(pageSize));
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Success));
        }

        // 실패 테스트도 OkResult 로 받고 response 의 ErrorCode 를 보고 판단한다.
        [Test, Order(1)]
        public async Task Get_ReturnsFailResultWithPaging()
        {
            int page = -2;
            int pageSize = 10;

            var actionResult = await this.controller.GetPagedEmplyeeList(page, pageSize);
            var okResult = actionResult.Result as OkObjectResult;

            Assert.NotNull(okResult);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.NotNull(okResult.Value);

            // 데이터 값 검증
            int totalPages = (int)Math.Ceiling((double)this.employees.Count / pageSize);

            var response = okResult.Value as GetPagedEmplyeeResponse;
            Assert.NotNull(response);

            Assert.That(response.TotalCount, Is.EqualTo(this.employees.Count));
            Assert.That(response.TotalPages, Is.EqualTo(totalPages));
            Assert.That(response.CurrentPage, Is.EqualTo(page));
            Assert.That(response.PageSize, Is.EqualTo(pageSize));
            Assert.That(response.Employees, Is.Null);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Invalid_Page_OR_PageSize));
        }

        [Test]
        public async Task Get_ReturnsOkResultWithName()
        {
            string name = "이무기";

            var actionResult = await this.controller.GetEmployee(name);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.NotNull(okResult.Value);

            var response = okResult.Value as GetEmployeeResponse;
            Assert.NotNull(response);

            // response.Employees 의 이름이 모두 name과 같아야 함
            foreach (var employee in response.Employees)
            {
                Assert.That(employee.name, Is.EqualTo(name));
            }
        }

        [Test]
        public async Task Get_ReturnsNotFoundResult()
        {
            string name = "John Doe";

            var actionResult = await this.controller.GetEmployee(name);
            var okResult = actionResult.Result as OkObjectResult;

            Assert.NotNull(okResult);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.NotNull(okResult.Value);

            var response = okResult.Value as GetEmployeeResponse;
            Assert.NotNull(response);
            Assert.That(response.Employees.Count(), Is.EqualTo(0));
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_NotExist_Employee));
        }

        // csv string 성공
        [Test]
        public async Task Post_ReturnsCreatedFromCsvResult()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "강철수,gangles@tmail.com,01025312468,2016.02.06\r\n" +
                    "구영희,gutilda@tmail.com,01017654321,2020.03.27"
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.NotNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Success));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount + 2));
        }

        // csv string 실패 (구조 오류로 인한 실패)
        [Test]
        public async Task Post_ReturnsCreatedFromCsvResultFail()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "강철수,gangles@tmail.com,01025312468,2016.02.06,,\r\n" +
                    "구영희,gutilda@tmail.com,01017654321,2020.03.27,,,,"
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Invalid_Data_Format));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        // csv 파일 성공
        [Test]
        public async Task Post_ReturnsCreatedFromCsvFileResult()
        {
            var stream = File.OpenRead(TestDataCsvPath);
            var fileName = Path.GetFileName(TestDataCsvPath);
            var fromFile = new FormFile(stream, 0, stream.Length, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            RegistMemberRequest request = new() 
            { 
                files = new FormFileCollection { fromFile },
                memberinfos = null
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.NotNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Success));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount + TestDataCsvFileLineCount));
        }

        // csv 파일 실패 (employee.json 파일을 읽고 시작하기 때문에 중복된 이메일로 실패 처리 테스트)
        [Test]
        public async Task Post_ReturnsCreatedFromCsvFileResultFail()
        {
            var stream = File.OpenRead(TestDataCsvFailPath);
            var fileName = Path.GetFileName(TestDataCsvFailPath);
            var fromFile = new FormFile(stream, 0, stream.Length, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            RegistMemberRequest request = new()
            {
                files = new FormFileCollection { fromFile },
                memberinfos = null
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Already_Exist_Email));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        // json string 성공
        [Test]
        public async Task Post_ReturnsCreatedFromJsonResult()
        {
            var employee = new Employee { name = "김대희", email = "daehee.kim@tmail.com", joined = DateTime.UtcNow, tel = "010-6528-0153" };
            var jsonString = "[" + JsonConvert.SerializeObject(employee) + "]";

            var request = new RegistMemberRequest
            {
                memberinfos = jsonString
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.NotNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Success));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount + 1));
        }

        // json string 실패 (뒤에 ] 삭제하여 에러 반출
        [Test]
        public async Task Post_ReturnsCreatedFromJsonResultFail()
        {
            var employee = new Employee { name = "김대희", email = "daehee.kim@tmail.com", joined = DateTime.UtcNow, tel = "010-6528-0153" };
            var jsonString = "[" + JsonConvert.SerializeObject(employee);

            var request = new RegistMemberRequest
            {
                memberinfos = jsonString
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Invalid_Data_Format));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        // json 파일 성공
        [Test]
        public async Task Post_ReturnsCreatedFromJsonFileResult()
        {
            var stream = File.OpenRead(TestDataJsonAddPath);
            var fileName = Path.GetFileName(TestDataJsonAddPath);
            var fromFile = new FormFile(stream, 0, stream.Length, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/json"
            };

            RegistMemberRequest request = new()
            {
                files = new FormFileCollection { fromFile },
                memberinfos = null
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.NotNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Success));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount + TestDataJsonFileAddLineCount));
        }

        // json 파일 실패 (json 형식 오류로 인한 Deserialize시 Exception 발생 ErrorCode.Unknown)
        [Test]
        public async Task Post_ReturnsCreatedFromJsonFileResultFail()
        {
            var stream = File.OpenRead(TestDataJsonFailPath);
            var fileName = Path.GetFileName(TestDataJsonFailPath);
            var fromFile = new FormFile(stream, 0, stream.Length, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/json"
            };

            RegistMemberRequest request = new()
            {
                files = new FormFileCollection { fromFile },
                memberinfos = null
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.Unknown));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        [Test]
        public async Task Post_ReturnsCreatedFailEmailFormat()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "강철수,gangles!tmail.com,01025312468,2016.02.06\r\n" +
                    "구영희,gutilda@tmail.com,01017654321,2020.03.27"
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Invalid_Email_Format));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        [Test]
        public async Task Post_ReturnCreatedFailTelFormat()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "강철수,gangles@tmail.com,1025312468,2016.02.06\r\n" +
                    "구영희,gutilda@tmail.com,010-1765-321,2020.03.27"
            };

            var prevTotalCount = await this.dbContext.GetTotalCountAsync();
            var actionResult = await this.controller.RegistMember(request);

            var createdResult = actionResult.Result as CreatedResult;
            var response = createdResult.Value as RegistMemberResponse;

            Assert.NotNull(response);
            Assert.IsNull(response.Employees);
            Assert.That(response.ErrorCode, Is.EqualTo((int)EErrorCode.ERR_Invalid_Tel_Format));
            Assert.That(createdResult.StatusCode, Is.EqualTo(201));
            Assert.That(await this.dbContext.GetTotalCountAsync(), Is.EqualTo(prevTotalCount));
        }

        private List<Employee> GetSampleEmployees()
        {
            // JSON 파일을 읽어서 List<Employee>로 변환한다
            string rawData = File.ReadAllText(TestDataJsonPath);
            var employees = JsonConvert.DeserializeObject<List<Employee>>(rawData);
            return employees;
        }
    }
}