using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WeddingApp.Api.Data;
using WeddingApp.Api.Dtos;
using WeddingApp.Api.Models;
using AutoMapper;

namespace WeddingApp.Api.Controllers {
    [Route ("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController (IAuthRepository repo, IConfiguration config, IMapper mapper) {
            _mapper = mapper;
            _config = config;
            _repo = repo;
        }

        [HttpPost ("register")]
        public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto) {

            //validation req
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower ();
            // if (string.IsNullOrEmpty(userForRegisterDto.Username) || string.IsNullOrEmpty(userForRegisterDto.Password))
            // {
            //     return BadRequest("Invalid username or password.");
            // }
            if (await _repo.UserExist (userForRegisterDto.Username))
                return BadRequest ("Username already exists");

            var usertoCreate = new User {

                Username = userForRegisterDto.Username

            };

            var createdUser = await _repo.Register (usertoCreate, userForRegisterDto.Password);
            return StatusCode (201);
        }

        [HttpPost ("login")]

        public async Task<IActionResult> Login (UserForLoginDto userForLoginDto) {
            var userFromRepo = await _repo.Login (userForLoginDto.Username.ToLower (), userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized ();

            var claims = new [] {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.Id.ToString ()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey (Encoding.UTF8
                .GetBytes (_config.GetSection ("AppSettings:Token").Value));

            var creds = new SigningCredentials (key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity (claims),
                Expires = DateTime.Now.AddDays (1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler ();

            var token = tokenHandler.CreateToken (tokenDescriptor);

            var user = _mapper.Map<UserForListDto>(userFromRepo);

            return Ok (new {
                token = tokenHandler.WriteToken(token),
                user
            });
        }
    }
}