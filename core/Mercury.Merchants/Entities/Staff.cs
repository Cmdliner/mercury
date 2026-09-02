namespace Mercury.Merchants.Entities;

public class Staff
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? StoreId { get; private set; }
    public Guid MerchantId { get; set; }
    public Guid IdentityUserId { get; init; }
    public  required string Name { get; set; }
    public StaffRole Role { get; private set; }


    private Staff()
    {
    }

    public static Staff Create(
        string name, 
        StaffRole role,
        Guid merchantId, 
        Guid identityUserId, 
        Guid? storeId)
    {
        if (role != StaffRole.Owner && !storeId.HasValue)
        {
            throw new ArgumentException($"{role} must be assigned to a store.");
        }
        if (role == StaffRole.Owner && storeId.HasValue)
        {
            throw new ArgumentException("Owner must not be scoped to a single store.");
        }

        return new Staff
        {
            Name = name,
            IdentityUserId = identityUserId,
            Role = role,
            MerchantId = merchantId,
            StoreId = storeId
        };
    }
}   