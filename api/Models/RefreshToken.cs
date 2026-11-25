using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Models;

// Token yang sama tidak boleh direkodkan dua kali.
[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    // Database menyimpan hash sahaja, bukan refresh token asal.
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    // Null = aktif. Ada nilai = token sudah direvoke.
    public DateTime? RevokedAt { get; set; }
}
