using Kitch.Application.Interfaces;

namespace Kitch.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verificar(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
