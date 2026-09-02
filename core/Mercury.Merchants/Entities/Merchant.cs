namespace Mercury.Merchants.Entities;

public class Merchant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string  Name { get; set; }
    
    public ICollection<Store> Stores { get; set; } = [];

    public ICollection<Staff> StaffMembers { get; set; } = [];
}

// N.B => THE MERCHANT IS THE BUSINESS ENTITY ITSELF. E.G NEURA-AID PHARMACEUTICALS