using Microsoft.AspNetCore.Identity;

namespace OtpAuth.Infrastructure.Identity;

/// <summary>
/// Microsoft Identity kullanıcısı. Şifresiz giriş kullanıldığı için parola alanları boş kalır;
/// kullanıcı yalnızca telefon numarası ile tanımlanır.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; }
}
