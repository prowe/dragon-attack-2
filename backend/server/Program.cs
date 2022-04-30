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
            services.AddHttpContextAccessor();

            services.AddGraphQLServer()
                .BindRuntimeType<Guid, IdType>()
                .AddSocketSessionInterceptor<CurrentPlayerInterceptor>()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSubscriptionType<Subscription>()
                .AddType<HealthChangedEvent>()
                .AddType<CharacterEnteredAreaEvent>();

            services.AddHostedService<DragonSpawner>();
        }

        private static void ConfigureOrleans(WebApplicationBuilder builder)
        {
            builder.Host.UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.AddAzureQueueStreams("default", optionsBuilder => 
                {
                    optionsBuilder.Configure(options=> {
                        var queueConnectionString = builder.Configuration.GetConnectionString("StreamProvider");
                        options.ConfigureQueueServiceClient(queueConnectionString);
                    });
                });
                siloBuilder.AddAzureTableGrainStorage("PubSubStore", options =>
                {
                    options.UseJson = true;
                    options.ConfigureTableServiceClient(builder.Configuration.GetConnectionString("GrainStorage"));
                });
                siloBuilder.AddAzureTableGrainStorageAsDefault(options =>
                {
                    options.UseJson = true;
                    options.ConfigureTableServiceClient(builder.Configuration.GetConnectionString("GrainStorage"));
                });
            });
        }
    }
}

