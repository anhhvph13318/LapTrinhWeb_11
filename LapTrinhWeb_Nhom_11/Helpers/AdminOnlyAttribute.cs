using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string? role = context.HttpContext.Session.GetString("Role");
            if (role != "ADMIN")
            {
                string accept = context.HttpContext.Request.Headers["Accept"].ToString();
                if (accept.Contains("text/html"))
                {
                    context.Result = new RedirectResult("/login");
                }
                else
                {
                    context.Result = new StatusCodeResult(401);
                }
            }
        }
    }
}
