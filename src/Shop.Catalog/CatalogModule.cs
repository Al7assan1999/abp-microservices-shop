using Microsoft.EntityFrameworkCore;
using Shop.Catalog.Data;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace Shop.Catalog;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AbpSwashbuckleModule))]
public class CatalogModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<CatalogDbContext>(o => o.AddDefaultRepositories(includeAllEntities: true));
        Configure<AbpDbContextOptions>(o => o.UseNpgsql());

        Configure<AbpDistributedEventBusOptions>(o =>
        {
            o.Outboxes.Configure(c => c.UseDbContext<CatalogDbContext>());
            o.Inboxes.Configure(c => c.UseDbContext<CatalogDbContext>());
        });

        Configure<AbpAspNetCoreMvcOptions>(o => o.ConventionalControllers.Create(typeof(CatalogModule).Assembly));

        context.Services.AddAbpSwaggerGen(o => o.CustomSchemaIds(t => t.FullName));
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        using (var scope = context.ServiceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        }

        var app = context.GetApplicationBuilder();
        app.UseRouting();
        app.UseUnitOfWork();
        app.UseSwagger();
        app.UseAbpSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API"));
        app.UseConfiguredEndpoints();
    }
}
