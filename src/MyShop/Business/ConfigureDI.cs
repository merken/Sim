namespace MyShop.Business;

public static class ConfigureDI
{
    public static IServiceCollection AddGlobalCart(this IServiceCollection services)
    {
        services.AddSingleton<Cart>();
        return services;
    }

    public static IServiceCollection AddPaymentService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddHttpClient<IPaymentService, PaymentService>(client =>
        {
            client.BaseAddress = new Uri(configuration["PaymentApi"]);
        });
        return services;
    }

    public static IServiceCollection AddProductCatalogService(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddHttpClient<IProductCatalogService, ProductCatalogService>(client =>
        {
            client.BaseAddress = new Uri(configuration["ProductsApi"]);
        });
        return services;
    }

    public static IServiceCollection AddCartService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICartService, CartService>();
        services.AddHttpClient<ICartService, CartService>(client =>
        {
            client.BaseAddress = new Uri(configuration["CartApi"]);
        });
        return services;
    }
}
