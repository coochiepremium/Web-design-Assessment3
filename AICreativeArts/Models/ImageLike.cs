using System.ComponentModel.DataAnnotations;

namespace AICreativeArts.Models;

/// <summary>
/// Records that one login session liked one image. The brief allows one like per image
/// per login, so a new row is allowed only after the user logs out and back in (each
/// login gets a new <see cref="LoginId"/>). A unique index on (LoginId, AIImageId)
/// stops a double like, even from two clicks at the same moment.
/// </summary>
public class ImageLike
{
    public int Id { get; set; }

    public int AIImageId { get; set; }

    public AIImage? AIImage { get; set; }

    /// <summary>The per-login identifier stored in the sign-in cookie (see Authorization/LoginSession.cs).</summary>
    [Required]
    [StringLength(64)]
    public string LoginId { get; set; } = "";

    /// <summary>The Identity user id of the member who liked the image.</summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = "";

    public DateTime LikedAt { get; set; }
}
