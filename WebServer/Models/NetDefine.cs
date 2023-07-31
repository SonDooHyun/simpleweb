using System.ComponentModel;

namespace WebServer.Models
{
    #region Request, Response

    public class BaseResponse
    {
        /// <summary>
        /// 에러코드 0 = Success
        /// </summary>
        public int ErrorCode { get; set; } = 0;

        /// <summary>
        /// 에러코드 설명
        /// </summary>
        public string ErrorDescription { get; set; } = "Success";
    }

    public sealed class RegistMemberRequest
    {
        /// <summary>
        /// 전송할 파일 (여러 개 가능) (CSV, JSON 파일만 가능)
        /// </summary>
        public IFormFileCollection? files { get; set; }

        /// <summary>
        /// 전송할 텍스트 (멀티 ROW 가능) (CSV, JSON 포맷만 가능)
        /// </summary>
        public string? memberinfos { get; set; }
    }

    public sealed class RegistMemberResponse : BaseResponse
    {
        /// <summary>
        /// 등록된 직원들 정보
        /// </summary>
        public IEnumerable<Employee>? Employees { get; set; }
    }

    public sealed class GetPagedEmplyeeResponse : BaseResponse
    {
        /// <summary>
        /// 총 직원 수
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 최대 페이지 수
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 현재 페이지
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 한 페이지에 들어가는 ROW 개수
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 조회된 직원들 정보
        /// </summary>
        public IEnumerable<Employee>? Employees { get; set; }
    }

    public sealed class GetEmployeeResponse : BaseResponse
    {
        /// <summary>
        /// 조회된 직원들 정보
        /// </summary>
        public IEnumerable<Employee>? Employees { get; set; }
    }

    #endregion

    public enum EErrorCode
    {
        [Description("알 수 없는 에러")]
        Unknown = -1,

        [Description("성공")]
        Success = 0,

        #region Regist Employee

        [Description("지원되지 않는 파일 형식입니다.")]
        ERR_Unknown_File_Format,

        [Description("지원되지 않는 데이터 형식입니다.")]
        ERR_Invalid_Data_Format,

        [Description("이미 존재하는 이메일 주소 입니다.")]
        ERR_Already_Exist_Email,

        [Description("이메일 양식이 잘못되었습니다.")]
        ERR_Invalid_Email_Format,

        [Description("전화번호 양식이 잘못되었습니다.")]
        ERR_Invalid_Tel_Format,
        #endregion

        #region Paged Employee List

        [Description("페이지 번호 혹은 페이지 크기 값이 잘못되었습니다.")]
        ERR_Invalid_Page_OR_PageSize,

        #endregion

        [Description("존재하지 않는 직원입니다.")]
        ERR_NotExist_Employee,
    }
}
