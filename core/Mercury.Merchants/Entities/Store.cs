namespace Mercury.Merchants.Entities;

public class Store
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MerchantId { get; private set; }
    public required string Name { get; set; }
    public required string Location { get;  set; }


    public ICollection<Staff> StaffMembers { get; private set; } = [];

}