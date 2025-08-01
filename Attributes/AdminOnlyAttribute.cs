using Microsoft.AspNetCore.Authorization;

namespace phoenix_sangam_api.Attributes;

public class SecretaryOnlyAttribute : AuthorizeAttribute
{
    public SecretaryOnlyAttribute()
    {
        Roles = "Secretary";
    }
} 