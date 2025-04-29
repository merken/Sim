using System.Text.Json;

namespace MyShop.Business;

public class CartTotals
{
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public interface ICartService
{
    Task AddToCart(Product product);
    Task RemoveFromCart(Product product);
    Task<List<Product>> GetCart();
    Task<CartTotals> GetTotals();
    Task Clear();
}

public class CartService(Cart cart, HttpClient client) : ICartService
{
    public async Task AddToCart(Product product)
    {
        cart.Products.Add(product);
    }

    public async Task RemoveFromCart(Product product)
    {
        var firstOrDefault = cart.Products.FirstOrDefault(p => p.Name == product.Name);
        if (firstOrDefault != null)
        {
            cart.Products.Remove(firstOrDefault);
        }
    }

    public async Task<List<Product>> GetCart()
    {
        if (cart.WasCartFetched)
            return cart.Products;

        var response = await client.GetAsync("api/cart");
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(responseContent, JsonSerializerOptions.Web);
            cart.Products.AddRange(products);
            cart.WasCartFetched = true;
            return cart.Products;
        }

        throw new Exception("Failed to fetch products from the API.");
    }

    public async Task<CartTotals> GetTotals()
    {
        if (!cart.WasCartFetched)
            await GetCart();

        return new CartTotals() { Total = cart.Products.Sum(p => p.Price), Count = cart.Products.Count };
    }

    public async Task Clear()
    {
        cart.Products.Clear();
    }
}
