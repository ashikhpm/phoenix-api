using Microsoft.AspNetCore.Authorization;

namespace phoenix_sangam_api.Attributes;

public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute()
    {
        Roles = "Admin";
    }
} 