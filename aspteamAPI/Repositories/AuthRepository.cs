using aspteamAPI.context;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using aspteamAPI.IRepositories;

namespace aspteamAPI.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthRepository(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Role = UserRole.JobSeeker,
                JobSeekerAccount = new JobSeekerAccount()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<AuthResponseDto> RegisterCompanyAsync(RegisterCompanyDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");

            var user = new User
            {
                Name = dto.CompanyName,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Role = UserRole.Company,
                CompanyAccount = new CompanyAccount
                {
                    CompanyName = dto.CompanyName,
                    Industry = dto.Industry,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<AuthResponseDto> LoginJobSeekerAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.JobSeekerAccount)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == UserRole.JobSeeker);

            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<AuthResponseDto> LoginCompanyAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.CompanyAccount)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == UserRole.Company);

            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };
        }

        // FIXED: Secure password hashing with salt
        private string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[128 / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            // Hash the password with the salt using PBKDF2
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            ));

            // Store salt and hash together (salt:hash)
            return Convert.ToBase64String(salt) + ":" + hashed;
        }

        // FIXED: Proper password verification
        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Split the stored hash to get salt and hash
                var parts = storedHash.Split(':');
                if (parts.Length != 2) return false;

                var salt = Convert.FromBase64String(parts[0]);
                var hash = parts[1];

                // Hash the provided password with the same salt
                string computedHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8
                ));

                // Compare the hashes
                return hash == computedHash;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}