using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    // gắn [LoginRequired] lên controller hoặc action nào cần đăng nhập (khách hàng hay admin đều được)
    public class LoginRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? accountId = context.HttpContext.Session.GetInt32("AccountId");
            bool isLogin = false;
            if (accountId != null)
            {
                // kiểm tra lại trong database để tài khoản bị khóa thì mất quyền ngay
                AppDbContext appDbContext = new AppDbContext();
                isLogin = appDbContext.Accounts.Any(c => c.Id == accountId && c.IsActive);
                if (isLogin == false)
                {
                    context.HttpContext.Session.Clear();
                }
            }
            if (isLogin == false)
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
