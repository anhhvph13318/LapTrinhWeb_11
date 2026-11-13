using LapTrinhWeb_Nhom_11.Models;
using Microsoft.EntityFrameworkCore;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class OrderHelper
    {
        // Hủy đơn và cộng lại số lượng vào kho.
        // maxStatus: chỉ hủy khi trạng thái đơn <= maxStatus (khách hủy thì truyền 0, admin hủy thì truyền 2)
        // Trả về false nếu đơn không còn ở trạng thái được hủy (đã hủy, đã hoàn thành, hoặc vừa bị đổi trạng thái)
        // Đổi trạng thái bằng 1 câu UPDATE có điều kiện nên 2 người cùng hủy 1 lúc thì chỉ 1 người được cộng kho
        // Hàm này phải gọi bên trong transaction (BeginTransaction) để đổi trạng thái và cộng kho cùng thành công hoặc cùng hủy
        public static bool CancelAndRestock(AppDbContext appDbContext, int orderId, int maxStatus)
        {
            int affected = appDbContext.Orders
                .Where(c => c.Id == orderId && c.Status <= maxStatus)
                .ExecuteUpdate(s => s.SetProperty(c => c.Status, 4));
            if (affected == 0)
            {
                return false;
            }
            var details = appDbContext.OrderDetails.Where(c => c.OrderId == orderId).ToList();
            foreach (var d in details)
            {
                int detailId = d.ProductDetailId;
                int quantity = d.Quantity;
                appDbContext.ProductDetails
                    .Where(c => c.Id == detailId)
                    .ExecuteUpdate(s => s.SetProperty(c => c.Quantity, c => c.Quantity + quantity));
            }
            return true;
        }

        // Đơn VNPay đặt quá 20 phút mà chưa thanh toán thì hủy và trả lại kho
        // (link thanh toán VNPay chỉ sống 15 phút nên sau 20 phút chắc chắn không thanh toán được nữa)
        // Không có tiến trình chạy nền nên hàm này được gọi mỗi khi có người đặt hàng hoặc xem danh sách đơn
        public static void ReleaseExpired()
        {
            try
            {
                AppDbContext appDbContext = new AppDbContext();
                DateTime limit = DateTime.Now.AddMinutes(-20);
                var ids = appDbContext.Orders
                    .Where(c => c.PaymentMethod == 1 && c.PaymentStatus == 0 && c.Status == 0 && c.CreatedAt < limit)
                    .Select(c => c.Id)
                    .ToList();
                foreach (int id in ids)
                {
                    using var tran = appDbContext.Database.BeginTransaction();
                    CancelAndRestock(appDbContext, id, 0);
                    tran.Commit();
                }
            }
            catch (Exception)
            {
                // dọn không được thì bỏ qua, lần sau dọn tiếp, không để lỗi này làm hỏng việc đặt hàng
            }
        }
    }
}