using WebServer.Log;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace WebServer.Helpers
{
    public class FuncCallLogAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string logMessage = $"[REQ : {context.HttpContext.Request.Method} {context.HttpContext.Request.Path} ";

            foreach (var args in context.ActionArguments)
            {
                string value = args.Value switch
                {
                    null => "null",
                    string str => str,
                    _ => JsonConvert.SerializeObject(args.Value)
                };
                logMessage += $"{args.Key} : {value} ";
            }

            logMessage += "]";

            AppLog.Info(logMessage);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            AppLog.Info($"[RES : {context.HttpContext.Request.Path} Result : {JsonConvert.SerializeObject(context.Result)}]");
        }
    }
}
