using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMangoApi.Data;
using RedMangoApi.Models;

namespace RedMangoApi.Controllers
{
    [Route("api/ShoppingCart")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        protected ApiResponse _response;

        public ShoppingCartController(ApplicationDbContext context)
        {
            _context = context;
            _response = new();
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemsInCart(string userId, int menuItemId, int updateQuantityBy)
        {
            ShoppingCart shoppingCart = await _context.ShoppingCarts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId.ToLower() == userId.ToLower());
            MenuItem menuItem = await _context.MenuItems.FirstOrDefaultAsync(mi => mi.Id == menuItemId);
            
            if(menuItem == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                return BadRequest(_response);
            }
            if(shoppingCart == null && updateQuantityBy > 0)
            {
                //create shopping cart and add cart item
                ShoppingCart newCart = new ShoppingCart()
                {
                    UserId = userId
                };
                await _context.ShoppingCarts.AddAsync(newCart);
                await _context.SaveChangesAsync();

                CartItem newCartItem = new()
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = newCart.Id,
                    MenuItem = null
                };
                await _context.CartItems.AddAsync(newCartItem);
                await _context.SaveChangesAsync();
            }

            else
            {
                //shopping cart exits
                CartItem cartItemInCart = shoppingCart.CartItems.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if(cartItemInCart == null)
                {
                    //item doesnt exist in current cart
                    CartItem newCartItem = new()
                    {
                        MenuItemId = menuItemId,
                        Quantity = updateQuantityBy,
                        ShoppingCartId = shoppingCart.Id,
                        MenuItem = null
                    };
                    await _context.CartItems.AddAsync(newCartItem);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    //item already exists in current cart and we have to update quantity
                    int newQuantity = cartItemInCart.Quantity + updateQuantityBy;
                    if(updateQuantityBy == 0 || newQuantity <= 0)
                    {
                        _context.CartItems.Remove(cartItemInCart);
                        if(shoppingCart.CartItems.Count() == 1)
                        {
                            _context.ShoppingCarts.Remove(shoppingCart);
                        }
                        _context.SaveChanges();
                    }
                    else
                    {
                        cartItemInCart.Quantity = newQuantity;
                        _context.SaveChanges();
                    }
                }

            }
            return _response;
        }
    }
}
