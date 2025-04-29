using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sim.Api.Tests;

internal class ApiSimulatorWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly IConfiguration _config;

    internal ApiSimulatorWebApplicationFactory(IConfiguration config)
    {
        _config = config;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => { config.AddConfiguration(_config); });

        return base.CreateHost(builder);
    }
}
