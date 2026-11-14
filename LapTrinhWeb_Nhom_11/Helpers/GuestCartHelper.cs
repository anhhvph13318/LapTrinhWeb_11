using LapTrinhWeb_Nhom_11.Models;
using System.Text.Json;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class GuestCartItem
    {
        public int ProductDetailId { get; set; }

        public int Quantity { get; set; }
    }

    public class GuestCartHelper
    {
        private const string Key = "GuestCart";

        public static int? GetLoginId(ISession session)
        {
            int? accountId = session.GetInt32("AccountId");
            if (accountId == null)
            {
                return null;
            }
            AppDbContext appDbContext = new AppDbContext();
            bool isLogin = appDbContext.Accounts.Any(c => c.Id == accountId && c.IsActive);
            if (isLogin == false)
            {
                session.Clear();
                return null;
            }
            return accountId;
        }

        public static List<GuestCartItem> Get(ISession session)
        {
            string? json = session.GetString(Key);
            if (string.IsNullOrEmpty(json))
            {
                return new List<GuestCartItem>();
            }
            return JsonSerializer.Deserialize<List<GuestCartItem>>(json) ?? new List<GuestCartItem>();
        }

        public static void Save(ISession session, List<GuestCartItem> items)
        {
            if (items.Count == 0)
            {
                session.Remove(Key);
                return;
            }
            session.SetString(Key, JsonSerializer.Serialize(items));
        }

        public static void Remove(ISession session, List<int> productDetailIds)
        {
            var items = Get(session);
            items = items.Where(c => productDetailIds.Contains(c.ProductDetailId) == false).ToList();
            Save(session, items);
        }

        public static void MergeToAccount(ISession session, AppDbContext appDbContext, int accountId)
        {
            var guestItems = Get(session);
            if (guestItems.Count == 0)
            {
                return;
            }
            var cart = appDbContext.Carts.Where(c => c.AccountId == accountId).FirstOrDefault();
            if (cart == null)
            {
                cart = new Cart();
                cart.AccountId = accountId;
                cart.CreatedAt = DateTime.Now;
                cart.UpdatedAt = DateTime.Now;
                appDbContext.Carts.Add(cart);
                appDbContext.SaveChanges();
            }
            foreach (var guestItem in guestItems)
            {
                var detail = appDbContext.ProductDetails.Where(c => c.Id == guestItem.ProductDetailId)
                    .Select(c => new { c.Quantity, c.Product.IsDeleted, c.Product.Status })
                    .FirstOrDefault();
                if (detail == null || detail.IsDeleted || detail.Status != 1)
                {
                    continue;
                }
                var item = appDbContext.CartItems.Where(c => c.CartId == cart.Id && c.ProductDetailId == guestItem.ProductDetailId).FirstOrDefault();
                int oldQuantity = item == null ? 0 : item.Quantity;
                int total = oldQuantity + guestItem.Quantity;
                if (total > detail.Quantity)
                {
                    total = detail.Quantity;
                }
                if (total < 1 || total < oldQuantity)
                {
                    continue;
                }
                if (item == null)
                {
                    item = new CartItem();
                    item.CartId = cart.Id;
                    item.ProductDetailId = guestItem.ProductDetailId;
                    item.Quantity = total;
                    item.CreatedAt = DateTime.Now;
                    appDbContext.CartItems.Add(item);
                }
                else
                {
                    item.Quantity = total;
                }
            }
            cart.UpdatedAt = DateTime.Now;
            appDbContext.SaveChanges();
            session.Remove(Key);
        }
    }
}
