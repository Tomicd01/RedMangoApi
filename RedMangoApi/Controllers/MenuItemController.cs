using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMangoApi.Data;
using RedMangoApi.Models;
using RedMangoApi.Models.Dto;
using RedMangoApi.Services;
using RedMangoApi.Utility;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace RedMangoApi.Controllers
{
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private ApiResponse _response;
        private readonly IBlobService _blobService;
        public MenuItemController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _response = new ApiResponse();
            _blobService = blobService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }


        [HttpGet("{id:int}", Name = "GetMenuItem")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            if(id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);

            }
            MenuItem item = await _context.MenuItems.FirstOrDefaultAsync(i => i.Id == id);
            if(item == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = item;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm]MenuItemCreateDto menuItemCreate)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if(menuItemCreate.File == null || menuItemCreate.File.Length == 0)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreate.File.FileName)}";
                    MenuItem menuItemToCreate = new()
                    {
                        Name = menuItemCreate.Name,
                        Price = menuItemCreate.Price,
                        Category = menuItemCreate.Category,
                        SpecialTag = menuItemCreate.SpecialTag,
                        Description = menuItemCreate.Description,
                        Image = await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemCreate.File)
                    };

                    _context.MenuItems.Add(menuItemToCreate);
                    await _context.SaveChangesAsync();
                    _response.Result = menuItemToCreate;
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItemToCreate.Id }, _response);
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }

            return _response;
        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UploadMenuItem(int id, [FromForm]MenuItemUpdateDto menuItemUpdateDto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDto == null || id != menuItemUpdateDto.Id)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                    MenuItem item = await _context.MenuItems.FindAsync(id);

                    if(item == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                    item.Name = menuItemUpdateDto.Name;
                    item.Price = menuItemUpdateDto.Price;
                    item.Description = menuItemUpdateDto.Description;
                    item.Category = menuItemUpdateDto.Category;
                    item.SpecialTag = menuItemUpdateDto.SpecialTag;

                    if(menuItemUpdateDto.File != null && menuItemUpdateDto.File.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDto.File.FileName)}";
                        await _blobService.DeleteBlob(item.Image.Split('/').Last(), SD.SD_Storage_Container); // obrisi blob sa imenom (parametar levo) iz kontejnera (par desno)
                        item.Image = await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemUpdateDto.File); // na mestu itema, unesi novi image sa imenom filename u kontejner, fajl koji je treci parametar
                    }

                    _context.MenuItems.Update(item);
                    await _context.SaveChangesAsync();
                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }

            return _response;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                MenuItem item = await _context.MenuItems.FindAsync(id);
                
                if(item == null || id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                await _blobService.DeleteBlob(item.Image.Split('/').Last(), SD.SD_Storage_Container);

                _context.MenuItems.Remove(item);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.NoContent;
                await _context.SaveChangesAsync();

                return Ok(_response);
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() }; 
            }

            return _response;
        }

    }
}
