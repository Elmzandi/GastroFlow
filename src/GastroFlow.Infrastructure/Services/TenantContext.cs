using Microsoft.AspNetCore.Http;

public class TenantContext : ITenantContext
{
    public Guid RestaurantId { get;  }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
         var claim = httpContextAccessor.HttpContext?.User
            .FindFirst("restaurantId")?.Value;

        // Guid.Empty during register/login — no JWT exists yet
        RestaurantId = Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}