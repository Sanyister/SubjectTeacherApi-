﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using zsoow6TeacherSubjApi.Model.DTO;
using zsoow6TeacherSubjApi.Model.Entity;
using zsoow6TeacherSubjApi.Services;

namespace zsoow6TeacherSubjApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthenticationController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthenticationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IUserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _userService = userService;
        }

        /// <summary>
        /// Register user by the provided registration data and return the status based on, whether the process was successful or not
        /// </summary>
        /// <param name="userForRegistration"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationDTO userForRegistration)
        {
            if (_userManager.Users.Any(u => u.UserName == userForRegistration.Username || u.Email == userForRegistration.Email))
            {
                throw new ApplicationException("Username/Email already exists!");
            }
            var user = new ApplicationUser {
                UserName = userForRegistration.Username,
                Email = userForRegistration.Email,
                DateOfBirth = userForRegistration.DateOfBirth,
                Name = userForRegistration.Name,
                NeptunCode = userForRegistration.NeptunCode,
                Department = userForRegistration.Department
                };
            var result = await _userManager.CreateAsync(user, userForRegistration.Password);
            return result.Succeeded ? StatusCode(201) : throw new ApplicationException("Registration failed!");
        }

        /// <summary>
        /// Login the user by username and password and return a generated JWT token
        /// </summary>
        /// <param name="userLoginDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLoginDTO)
        {
            var user = await _userManager.FindByNameAsync(userLoginDTO.Username);
            
            if (user != null && await _userManager.CheckPasswordAsync(user, userLoginDTO.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Actor, user.IsUser.ToString())
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(4),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        [HttpPost]
        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        /// <summary>
        /// Create Admin and User default roles
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> InitRoles()
        {
            await _userService.InitRoles();
            return Ok();
        }

        /// <summary>
        /// Create two user one with Admin role and one with User role
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> InitUsers()
        {
            await _userService.InitUsers();
            return Ok();
        }
    }
}
