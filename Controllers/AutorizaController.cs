using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using apiUniversidade.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;


namespace apiUniversidade.Controllers;


[ApiController]
[Route("[controller]")]
public class AutorizaController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;


    public AutorizaController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }


    private UsuarioToken GeraToken(UsuarioDTO userInfo)
    {
        var claims = new[]
        {
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName,userInfo.Email),
            new Claim("BFR","Star"),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
        };


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]));


        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


        var expiracao = _configuration["TokenConfiguration:ExpireHours"];
        var expiration = DateTime.UtcNow.AddHours(double.Parse(expiracao));


        JwtSecurityToken token = new JwtSecurityToken
        (
            issuer: _configuration["TokenConfiguration:Issuer"],
            audience: _configuration["TokenConfiguration:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );


        return new UsuarioToken()
        {
            Athenticated = true,
            Expiration = expiration,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Message = "JWT Ok."
        };
    }


    [HttpGet]
    public ActionResult<string> Get()
    {
        return "AutorizaController :: Acessado em : " + DateTime.Now.ToLongDateString();
    }


    [HttpPost("register")]
    public async Task<ActionResult> RegisterUser([FromBody] UsuarioDTO model)
    {
        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };


        var result = await _userManager.CreateAsync(user, model.Password);
        if(!result.Succeeded)
            return BadRequest(result.Errors);
       
        await _signInManager.SignInAsync(user, false);
        return Ok(GeraToken(model));
    }


    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] UsuarioDTO userInfo)
    {
        var result = await _signInManager.PasswordSignInAsync
        (
            userInfo.Email,
            userInfo.Password,
            isPersistent: false,
            lockoutOnFailure: false
        );


        if(result.Succeeded)
            return Ok(GeraToken(userInfo));
        else
        {
            ModelState.AddModelError(string.Empty, "Login Inválido...");
            return BadRequest(ModelState);
        }
    }
}
