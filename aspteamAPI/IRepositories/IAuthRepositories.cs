namespace aspteamAPI.IRepositories
{
    public interface IAuthRepository
    {
        Task<AuthResponseDto> RegisterJobSeekerAsync(RegisterJobSeekerDto dto);
        Task<AuthResponseDto> RegisterCompanyAsync(RegisterCompanyDto dto);
        Task<AuthResponseDto> LoginJobSeekerAsync(LoginDto dto);
        Task<AuthResponseDto> LoginCompanyAsync(LoginDto dto);
    }
}
