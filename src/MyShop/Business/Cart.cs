namespace MyShop.Business;

public class Cart
{
    public bool WasCartFetched { get; set; } = false;
    public List<Product> Products { get; set; } = new();
}
