using System.ComponentModel.DataAnnotations.Schema;

namespace RedMangoApi.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public ICollection<CartItem> CartItems { get; set; } // 1:N relationship. 1 shopping cart can have many cart items

        [NotMapped]
        public double CartTotal { get; set; }
        [NotMapped]
        public string StripePaymentIntentId { get; set; }
        [NotMapped]
        public string ClientSecret { get; set; }
    }

}
