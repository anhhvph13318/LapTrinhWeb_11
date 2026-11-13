using LapTrinhWeb_Nhom_11.Models;
using Microsoft.EntityFrameworkCore;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class OrderHelper
    {
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
            }
        }
    }
}