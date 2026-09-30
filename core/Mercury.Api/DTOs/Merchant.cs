using System.ComponentModel.DataAnnotations;

namespace Mercury.Api.DTOs;

public record StoreCreateRequest(
    [Required,  MaxLength(50)] string Name,
    [Required, MaxLength(128)] string Location);
    
    
public record StoreCreateResponse([Required, MaxLength(128)] string Name, Guid StoreId);

public record StoreResponse(Guid Id,  string Name, string Location);

public record StoresListResponse(string MerchantName, IEnumerable<StoreResponse> Stores);
