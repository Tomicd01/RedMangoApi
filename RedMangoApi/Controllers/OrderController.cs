using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMangoApi.Data;
using RedMangoApi.Models;
using RedMangoApi.Services;

namespace RedMangoApi.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private ApiResponse _response;
        private readonly IBlobService _blobService;
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetOrders(string? userId)
        {
            try
            {
                var orderHeaders = _context.OrderHeaders
                    .Include(oh => oh.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                    .OrderByDescending(oh => oh.OrderHeaderId);
                if (!string.IsNullOrEmpty(userId))
                {
                    _response.Result = orderHeaders.Where(oh => oh.ApplicationUserId == userId);
                }
                else
                {
                    _response.Result = orderHeaders;
                }

                _response.StatusCode = System.Net.HttpStatusCode.OK;
                return Ok(_response);
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message.ToString());
                return BadRequest(_response);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse>> GetOrderById(int id)
        {
            try
            {
                if(id == 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _response.ErrorMessages.Add("Id does not exist");
                    return BadRequest(_response);
                }
                
                var orderHeaders = _context.OrderHeaders
                    .Include(oh => oh.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                    .Where(oh => oh.OrderHeaderId == id);
                if (orderHeaders == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    _response.ErrorMessages.Add("Order not found");
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Result = orderHeaders;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add(ex.Message.ToString());
                return BadRequest(_response);
            }
        }
    }
}
