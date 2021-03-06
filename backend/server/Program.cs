using Orleans;
using Orleans.Hosting;

namespace DragonAttack
{
    public class Program
    {
        public static Task Main(string[] args)
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
            return app.RunAsync("http://0.0.0.0:5000");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSingleton<Mutation>();
            services.AddSingleton<Query>();
            services.AddSingleton<Subscription>();

            services
                .AddGraphQLServer()
                .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
                .AddDocumentFromFile("schema.graphql")
                .AddSocketSessionInterceptor<CurrentPlayerInterceptor>()
                .BindRuntimeType<Guid, IdType>()
                .BindRuntimeType<Query>()
                .BindRuntimeType<Subscription>()
                .BindRuntimeType<Mutation>()
                .BindRuntimeType<GameCharacter>()
                .BindRuntimeType<HealthChangedEvent>()
                .BindRuntimeType<CharacterEnteredAreaEvent>()
                .BindRuntimeType<CharacterExitedAreaEvent>()
            ;

            services.AddSingleton<IDictionary<Guid, Ability>>(BuildAbilityMap);
        }

        private static void ConfigureOrleans(WebApplicationBuilder builder)
        {
            builder.Host.UseOrleans(siloBuilder =>
            {
                var config = builder.Configuration;
                siloBuilder.UseLocalhostClustering();
                siloBuilder.AddStartupTask<DragonSpawner>();
                if (config.GetValue<bool>("UseAzureStorage", false))
                {
                    siloBuilder.AddAzureQueueStreams("default", optionsBuilder => 
                    {
                        optionsBuilder.Configure(options=> {
                            var queueConnectionString = config.GetConnectionString("StreamProvider");
                            options.ConfigureQueueServiceClient(queueConnectionString);
                        });
                    });
                    siloBuilder.AddAzureTableGrainStorage("PubSubStore", options =>
                    {
                        options.UseJson = true;
                        options.ConfigureTableServiceClient(config.GetConnectionString("GrainStorage"));
                    });
                    siloBuilder.AddAzureTableGrainStorageAsDefault(options =>
                    {
                        options.UseJson = true;
                        options.ConfigureTableServiceClient(config.GetConnectionString("GrainStorage"));
                    });
                }
                else
                {
                    siloBuilder.AddSimpleMessageStreamProvider("default");
                    siloBuilder.AddMemoryGrainStorageAsDefault();
                    siloBuilder.AddMemoryGrainStorage("PubSubStore");
                }
            });
        }

        private static IDictionary<Guid, Ability> BuildAbilityMap(IServiceProvider services)
        {
            var abilities = new []
            {
                new Ability
                {
                    Id = Guid.Parse("7d86e255-72b0-43e6-9d64-ec19d90ae353"),
                    Name = "Claw",
                    Effect = AbilityEffect.Damage,
                    Dice = new DiceSpecification { Rolls = 3, Sides = 6}
                },
                new Ability
                {
                    Id = Guid.Parse("666e12fa-9bb8-4420-b38e-37d987447633"),
                    Name = "Flame Breath",
                    Effect = AbilityEffect.Damage,
                    Dice = new DiceSpecification { Rolls = 4, Sides = 6},
                    MaxTargets = 1000,
                    Cooldown = TimeSpan.FromSeconds(14),
                },
                new Ability
                {
                    Id = Guid.Parse("566c8543-4ba1-4cdf-b921-b811c3a8db52"),
                    Name = "Slash",
                    Dice = new DiceSpecification { Rolls = 1, Sides = 6, Constant = 0},
                    Effect = AbilityEffect.Damage,
                    Cooldown = TimeSpan.FromSeconds(1),
                },
                new Ability
                {
                    Id = Guid.Parse("781c7a2a-21e0-4203-ad6d-045696250ff9"),
                    Name = "Heal",
                    Dice = new DiceSpecification { Rolls = 4, Sides = 8, Constant = 10},
                    Effect = AbilityEffect.Heal,
                    Cooldown = TimeSpan.FromSeconds(15),
                }
            };
            return abilities.ToDictionary(ab => ab.Id);
        }
    }
}

