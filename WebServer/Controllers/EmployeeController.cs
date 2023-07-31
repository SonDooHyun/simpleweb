using WebServer.Helpers;
using WebServer.Models;
using WebServer.Services;
using Microsoft.AspNetCore.Mvc;


namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/employee")]
    [Produces("application/json")]
    public class EmployeeController : ControllerBase
    {
        private IEmployeeService employeeService;

        public EmployeeController(IEmployeeService employeeManager)
        {
            this.employeeService = employeeManager;
        }

        /// <summary>
        /// 페이징된 직원 리스트
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/employee
        ///     {
        ///         "page": 2,
        ///         "pageSize" : 5
        ///     }
        ///     
        /// </remarks>
        /// <param name="page">데이터를 가져올 페이지 번호</param>
        /// <param name="pageSize">한 페이지당 데이터(ROW) 개수</param>
        [HttpGet]
        [FuncCallLog]
        public async Task<ActionResult<GetPagedEmplyeeResponse>> GetPagedEmplyeeList(int page, int pageSize)
        {
            var response = await this.employeeService.GetPagedEmployeesAsync(page, pageSize);
            return Ok(response);
        }

        /// <summary>
        /// 조회한 이름의 직원 정보 (복수가 될 수 있음)
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/employee/{name}
        ///     {
        ///         "name": "김철수"
        ///     }
        ///     
        /// </remarks>
        /// <param name="name">검색할 이름</param>
        [HttpGet("{name}")]
        [FuncCallLog]
        public async Task<ActionResult<GetEmployeeResponse>> GetEmployee(string name)
        {
            var response = await this.employeeService.GetEmployeeByNameAsync(name);
            return Ok(response);
        }

        /// <summary>
        /// 직원 등록
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     [string 직접 입력시]
        ///     POST /api/employee
        ///     {
        ///         "memberinfos": 송혜교,hyekyo.song@tmail.com,01078965231,2020.02.06
        ///     }
        ///     
        ///     [파일 선택시]
        ///     ../WebServer.Tests/TestData 안의 파일 선택 (employee.csv or employee.json)
        ///     
        /// </remarks>
        /// <param name="request"></param>
        [HttpPost]
        [FuncCallLog]
        public async Task<ActionResult<RegistMemberResponse>> RegistMember([FromForm] RegistMemberRequest request)
        {
            var response = await this.employeeService.RegistEmployee(request);

            return Created(nameof(GetEmployee), response);
        }
    }
}
