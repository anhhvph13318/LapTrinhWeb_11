using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    // gắn [AdminOnly] lên controller hoặc action nào chỉ cho admin vào
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
                    // mở trang bằng trình duyệt thì chuyển về trang đăng nhập
                    context.Result = new RedirectResult("/login");
                }
                else
                {
                    // gọi API bằng fetch thì trả về 401
                    context.Result = new StatusCodeResult(401);
                }
            }
        }
    }
}
