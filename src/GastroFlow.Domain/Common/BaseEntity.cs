namespace GastroFlow.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // Every entity that inherits BaseEntity belongs to one restaurant.
    // EF Core will use this to automatically filter all queries.
    // Restaurant A can never see Restaurant B's data. Ever.
    public Guid RestaurantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
}
