
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class LoginRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? accountId = context.HttpContext.Session.GetInt32("AccountId");

            bool isLogin = false;

            if (accountId.HasValue && accountId.Value > 0)
            {
                using (AppDbContext db = new AppDbContext())
                {
                    isLogin = db.Accounts.Any(c =>
                        c.Id == accountId.Value && c.IsActive);
                }

                if (!isLogin)
                {
                    context.HttpContext.Session.Clear();
                }
            }

            if (!isLogin)
            {
                var request = context.HttpContext.Request;

                bool isApi = request.Path.StartsWithSegments("/cart");

                if (isApi)
                {
                    context.Result = new UnauthorizedObjectResult(
                        new { message = "Phiên đăng nhập đã hết hạn" }
                    );
                }
                else
                {
                    string returnUrl = request.Path + request.QueryString;

                    context.Result = new RedirectResult(
                        "/login?returnUrl=" +
                        Uri.EscapeDataString(returnUrl)
                    );
                }
            }
        }
    }
}
