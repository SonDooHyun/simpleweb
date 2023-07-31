using WebServer.Helpers;
using System.Text.RegularExpressions;

namespace WebServer.Models
{
    public static class EmployeeExtensions
    {
        private const int TEL_LENGTH_WITHOUT_HYPEN = 11;

        public static void Validate(this Employee employee)
        {
            string email_pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            bool IsEnableEmail = Regex.IsMatch(employee.email, email_pattern);
            if (IsEnableEmail == false)
            {
                throw new AppException(EErrorCode.ERR_Invalid_Email_Format, employee.email);
            }

            string tel_pattern = @"^\d{3}-?\d{4}-?\d{4}$";
            bool IsEnableTel = Regex.IsMatch(employee.tel, tel_pattern);
            if (IsEnableTel == false)
            {
                throw new AppException(EErrorCode.ERR_Invalid_Tel_Format, employee.tel);
            }

            if (employee.tel.Length == TEL_LENGTH_WITHOUT_HYPEN &&
                employee.tel.Contains('-') == false)
            {
                employee.tel = employee.tel.Insert(3, "-").Insert(8, "-");
            }
        }
    }
}
