using Orleans;
using Orleans.Hosting;

namespace DragonAttack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureOrleans(builder);
            ConfigureServices(builder.Services);

            var app = builder.Build();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

            app.UseWebSockets();
            app.MapGraphQL();
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddTransient<INPCController, DragonController>();
            services.AddSingleton<Mutation>();
            services.AddSingleton<Query>();
            services.AddSingleton<Subscription>();

            services.AddGraphQLServer()
                .BindRuntimeType<Guid, IdType>()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSubscriptionType<Subscription>()
                .AddType<AttackedEvent>();

            services.AddHostedService<DragonSpawner>();
        }

        private static void ConfigureOrleans(WebApplicationBuilder builder)
        {
            builder.Host.UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.AddSimpleMessageStreamProvider("default");
                siloBuilder.AddMemoryGrainStorage("PubSubStore");
            });
        }
    }
}

