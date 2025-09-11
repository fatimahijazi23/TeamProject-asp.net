using aspteamAPI.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace aspteamAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
        public class AuthController : ControllerBase
        {
            private readonly IAuthRepository _repo;

            public AuthController(IAuthRepository repo)
            {
                _repo = repo;
            }

            [HttpPost("register-jobseeker")]
            public async Task<IActionResult> RegisterJobSeeker([FromBody] RegisterJobSeekerDto dto)
            {
                var result = await _repo.RegisterJobSeekerAsync(dto);
                return Ok(result);
            }

            [HttpPost("register-company")]
            public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyDto dto)
            {
                var result = await _repo.RegisterCompanyAsync(dto);
                return Ok(result);
            }

            [HttpPost("login-jobseeker")]
            public async Task<IActionResult> LoginJobSeeker([FromBody] LoginDto dto)
            {
                var result = await _repo.LoginJobSeekerAsync(dto);
                return Ok(result);
            }

            [HttpPost("login-company")]
            public async Task<IActionResult> LoginCompany([FromBody] LoginDto dto)
            {
                var result = await _repo.LoginCompanyAsync(dto);
                return Ok(result);
            }
        }
    }

