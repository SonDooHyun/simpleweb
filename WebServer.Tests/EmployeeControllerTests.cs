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

        // ���� �ٸ� �׽�Ʈ���� �߰��Ǹ� ����ġ ���� ����� �����Ƿ� ���� �׽�Ʈ ����
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

            // ������ �� ����
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

        // ���� �׽�Ʈ�� OkResult �� �ް� response �� ErrorCode �� ���� �Ǵ��Ѵ�.
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

            // ������ �� ����
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
            string name = "�̹���";

            var actionResult = await this.controller.GetEmployee(name);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            Assert.NotNull(okResult.Value);

            var response = okResult.Value as GetEmployeeResponse;
            Assert.NotNull(response);

            // response.Employees �� �̸��� ��� name�� ���ƾ� ��
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

        // csv string ����
        [Test]
        public async Task Post_ReturnsCreatedFromCsvResult()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "��ö��,gangles@tmail.com,01025312468,2016.02.06\r\n" +
                    "������,gutilda@tmail.com,01017654321,2020.03.27"
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

        // csv string ���� (���� ������ ���� ����)
        [Test]
        public async Task Post_ReturnsCreatedFromCsvResultFail()
        {
            var request = new RegistMemberRequest
            {
                memberinfos = "��ö��,gangles@tmail.com,01025312468,2016.02.06,,\r\n" +
                    "������,gutilda@tmail.com,01017654321,2020.03.27,,,,"
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

        // csv ���� ����
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

        // csv ���� ���� (employee.json ������ �а� �����ϱ� ������ �ߺ��� �̸��Ϸ� ���� ó�� �׽�Ʈ)
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

        // json string ����
        [Test]
        public async Task Post_ReturnsCreatedFromJsonResult()
        {
            var employee = new Employee { name = "�����", email = "daehee.kim@tmail.com", joined = DateTime.UtcNow, tel = "010-6528-0153" };
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

        // json string ���� (�ڿ� ] �����Ͽ� ���� ����
        [Test]
        public async Task Post_ReturnsCreatedFromJsonResultFail()
        {
            var employee = new Employee { name = "�����", email = "daehee.kim@tmail.com", joined = DateTime.UtcNow, tel = "010-6528-0153" };
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

        // json ���� ����
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

        // json ���� ���� (json ���� ������ ���� Deserialize�� Exception �߻� ErrorCode.Unknown)
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
                memberinfos = "��ö��,gangles!tmail.com,01025312468,2016.02.06\r\n" +
                    "������,gutilda@tmail.com,01017654321,2020.03.27"
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
                memberinfos = "��ö��,gangles@tmail.com,1025312468,2016.02.06\r\n" +
                    "������,gutilda@tmail.com,010-1765-321,2020.03.27"
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
            // JSON ������ �о List<Employee>�� ��ȯ�Ѵ�
            string rawData = File.ReadAllText(TestDataJsonPath);
            var employees = JsonConvert.DeserializeObject<List<Employee>>(rawData);
            return employees;
        }
    }
}