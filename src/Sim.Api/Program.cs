using System.Globalization;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Sim.Api;
using Sim.Api.Controllers;
using Sim.Api.Validators;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureApiVersioning();
builder.Services.AddSwaggerGen(options => { options.OperationFilter<SwaggerDefaultValues>(); });

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddSingleton<ISimulationsDataStore, SimulationsDataStore>();
builder.Services.AddSingleton<IValidator<SimulationsDataEntryModel>, SimulationsDataEntryModelValidator>();

builder.Services.AddScoped<DynamicControllerRouteTransformer>();

builder.Services.AddControllers();

builder.Services.AddHttpForwarder();
var app = builder.Build();
var versionSet = app.SetupApiVersionSet();
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseRouting();

app.UseMiddleware<ExceptionHandlingMiddleware>();

#pragma warning disable ASP0014
app.UseEndpoints(a =>
{
    a
        .MapControllers()
        .WithOpenApi();
    a.MapDynamicControllerRoute<DynamicControllerRouteTransformer>("{**route}");
});
#pragma warning restore ASP0014

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI((options) =>
{
    var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions;
    foreach (var description in descriptions)
    {
        var url = $"/swagger/{description.GroupName}/swagger.json";
        var name = description.GroupName.ToUpperInvariant();
        options.SwaggerEndpoint(url, name);
    }
});

app.Run();

public partial class Program
{
}

internal static class OpenApiSetup
{
    internal static void ConfigureApiVersioning(this IServiceCollection services) => _ = services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1.0);
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
        }).AddApiExplorer(options => { options.GroupNameFormat = "'v'V"; });

    internal static ApiVersionSet SetupApiVersionSet(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1.0))
            .ReportApiVersions()
            .Build();
        return versionSet;
    }
}

internal class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo()
        {
            Title = "ApiSimulator API",
            Version = description.ApiVersion.ToString(),
            Description = "Stop faking, start simulating",
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}

internal class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString(CultureInfo.InvariantCulture);
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameter.Name);
            if (description == null)
                continue;

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null &&
                description.DefaultValue != null &&
                description.DefaultValue is not DBNull &&
                description.ModelMetadata is ModelMetadata modelMetadata)
            {
                // REF: https://github.com/Microsoft/aspnet-api-versioning/issues/429#issuecomment-605402330
                var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
