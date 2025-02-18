using System.ComponentModel.DataAnnotations;

namespace Api;

public class AppOptions
{
    [Required(ErrorMessage = "hey buddy you need a REDIS_HOST in your appsettings.json under the AppOptions scope")]
    public string REDIS_HOST { get; set; } = null!;

    [Required(ErrorMessage = "hey buddy you need a REDIS_USERNAME in your appsettings.json under the AppOptions scope")]
    public string REDIS_USERNAME { get; set; } = null!;
    [Required(ErrorMessage = "hey buddy you need a REDIS_PASSWORD in your appsettings.json under the AppOptions scope")]
    public string REDIS_PASSWORD { get; set; } = null!;
}