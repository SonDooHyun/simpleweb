using WebServer.Models;
using System.ComponentModel;
using System.Reflection;

namespace WebServer.Helpers
{
    public static class ErrorDescription
    {
        public static string GetErrorString(this EErrorCode errorCode)
        {
            FieldInfo fi = errorCode.GetType().GetField(errorCode.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return errorCode.ToString();
            }
        }
    }
}
