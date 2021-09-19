using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.classes;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  public class AccountController : BaseApiController
  {
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;

    public AccountController(DataContext context, ITokenService tokenService)
    {
      _tokenService = tokenService;
      _context = context;


    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDto)
    {
      string username = registerDto.UserName.ToLower(); string password = registerDto.Password;
      if (UserExists(username).Result)
      {

        return BadRequest("User is Taken.");

      }
      using var hmac = new HMACSHA512();
      var user = new AppUser
      {
        UserName = username,
        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
        PasswordSalt = hmac.Key
      };
      _context.Users.Add(user);
      await _context.SaveChangesAsync();
return    new UserDTO{
UserName=user.UserName,Token=_tokenService.CreateToken(user)

      };

    }



    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDto LoginDto)
    {
      string username = LoginDto.UserName.ToLower(); string password = LoginDto.Password;
      AppUser user = await UserFetch(username);
      if (user == null)
      {
        return Unauthorized("Incorrect Credentials.");
      }
      using var hmac = new HMACSHA512(user.PasswordSalt);
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

      for (int i = 0; i < computedHash.Length; i++)
      {
        if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Incorrect Credentials.");
      }


      return new UserDTO{
UserName=user.UserName,Token=_tokenService.CreateToken(user)

      };

    }

    private async Task<AppUser> UserFetch(string username)
    {
      return await _context.Users.SingleOrDefaultAsync(user => user.UserName == username.ToLower());
    }

    private async Task<bool> UserExists(string username)
    {
      return await _context.Users.AnyAsync(user => user.UserName == username.ToLower());
    }


  }
}