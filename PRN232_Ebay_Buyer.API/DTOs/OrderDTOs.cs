namespace PRN232_Ebay_Buyer.API.DTOs;

public class CartItemDto
{
    public int ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class SyncCartRequest
{
    public List<CartItemDto> Items { get; set; } = new();
}

public class AddressDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class CreateAddressRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class CheckoutRequest
{
    public int AddressId { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public List<CartItemDto> Items { get; set; } = new();
}
