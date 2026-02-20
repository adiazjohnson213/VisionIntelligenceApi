using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VisionIntelligenceAPI.Services;
using VisionIntelligenceAPI.Tests.TestDoubles;

namespace VisionIntelligenceAPI.Tests
{
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remover registro real
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(VisionAnalysisService));
                if (descriptor is not null) services.Remove(descriptor);

                // Registrar fake
                services.AddScoped<VisionAnalysisService, FakeVisionAnalysisService>();
            });
        }
    }
}
