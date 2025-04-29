using System.Text.Json;

namespace MyShop.Business;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public interface IProductCatalogService
{
    Task<IEnumerable<Product>> GetProducts();
}

public class ProductCatalogService(HttpClient client) : IProductCatalogService
{
    public async Task<IEnumerable<Product>> GetProducts()
    {
        var response = await client.GetAsync("api/products");

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assuming the response is a JSON array of products
            var products = JsonSerializer.Deserialize<IEnumerable<Product>>(responseContent, JsonSerializerOptions.Web);
            return products;
        }

        throw new Exception("Failed to fetch products from the API.");
    }
}
