using System.ComponentModel.DataAnnotations;

namespace WebServer.Models
{
    public class Employee
    {
        [Key]
        public int id { get; set; }

        /// <summary>
        /// 이름
        /// </summary>
        /// <example>아무개</example>
        [Required]
        public string name { get; set; }

        /// <summary>
        /// 이메일 주소
        /// </summary>
        /// <example>abc@def.com</example>
        [Required]
        public string email { get; set; }

        /// <summary>
        /// 전화번호
        /// </summary>
        /// <example>01078945612</example>
        [Required]
        public string tel { get; set; }

        /// <summary>
        /// 가입 일자
        /// </summary>
        /// <example>2019-12-05 또는 2018.07.02</example>
        [Required]
        public DateTime joined { get; set; }

        public override int GetHashCode()
        {
            return this.email.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Employee;
            if (other == null)
                return false;

            return this.email == other.email;
        }
    }
}
