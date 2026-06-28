public abstract class BaseEntity
{
    public Guid Id { get; set; } = new Guid();
    // Every entity that inherits BaseEntity belongs to one restaurant.
    // EF Core will use this to automatically filter all queries.
    // Restaurant A can never see Restaurant B's data.
    public Guid RestaurantId { get; set; } = new Guid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } 
}