public class Restaurant 
{
    public Guid Id { get; set; } = new Guid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;
    public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}    