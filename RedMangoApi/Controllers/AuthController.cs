using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RedMangoApi.Data;
using RedMangoApi.Models;
using RedMangoApi.Models.Dto;
using RedMangoApi.Services;
using RedMangoApi.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security;
using System.Security.Claims;

namespace RedMangoApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private ApiResponse _response;
        private string secretKey;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(ApplicationDbContext context, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _response = new ApiResponse();
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequestDto model)
        {
            ApplicationUser user = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.UserName.ToLower() == model.UserName.ToLower());
            if(user != null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Username already exists.");
                return BadRequest(_response);
            }

            ApplicationUser newUser = new ()
            {
                Name = model.Name,
                UserName = model.UserName,
                NormalizedEmail = model.UserName.ToUpper(),
                Email = model.UserName
            };

            try
            {
                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
                    {
                        //if a role does not exist, here we create it
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
                    }

                    if (model.Role.ToLower() == SD.Role_Admin)
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Admin);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Customer);
                    }

                    _response.StatusCode = System.Net.HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
            

            }

            _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            _response.ErrorMessages.Add("Error while registering.");
            return BadRequest(_response);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromForm] LoginRequestDto model)
        {
            ApplicationUser user = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.UserName.ToLower() == model.Username.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isValid)
            {
                _response.Result = new LoginRequestDto();
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Wrong username or password entered.");
                return BadRequest(_response);
            }

            //If isValid is true, we generate jwt token
            var roles = await _userManager.GetRolesAsync(user);

            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(secretKey);
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("fullName", user.Name),
                    new Claim("id", user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.UserName.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                }),
                Expires = DateTime.UtcNow.AddDays(7), //token expires after 7 days
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);



            LoginResponseDto loginResponse = new LoginResponseDto()
            {
                Email = user.Email,
                Token = tokenHandler.WriteToken(token)
            };

            if(loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _response.IsSuccess = false;
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Wrong username or password entered.");
                return BadRequest(_response);
            }

            _response.IsSuccess = true;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            _response.Result = loginResponse;
            return Ok(_response);

        }
    }
}
