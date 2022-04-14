using System.Reflection;
using System.Runtime.ExceptionServices;
using GraphQL;
using GraphQL.Reflection;
using GraphQL.Resolvers;
using GraphQL.Server;
using GraphQL.Types;

namespace DragonAttack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder.Services);

            var app = builder.Build();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

            app.MapGet("/", () => "Hello World!");
            app.UseWebSockets();
            app.UseGraphQLWebSockets<ISchema>();
            app.UseGraphQL<ISchema>();
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSingleton<CounterHolder>();
            services.AddSingleton<MutationResolvers>();
            services.AddSingleton<QueryResolvers>();
            services.AddSingleton<SubscriptionResolvers>();

            services.AddSingleton<IDocumentExecuter, SubscriptionDocumentExecuter>();
            services.AddSingleton<ISchema>(LoadSchema);
            GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services)
                .AddServer(true)
                .ConfigureExecution(options =>
                {
                    options.EnableMetrics = true;
                    var logger = options.RequestServices!.GetRequiredService<ILogger<Program>>();
                    options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                })
                .AddSystemTextJson()
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
                .AddWebSockets();
        }

        private static Schema LoadSchema(IServiceProvider services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("server.schema.graphql");
            if (stream == null)
            {
                throw new NullReferenceException($"Cannot find schema among: {string.Join(',', assembly.GetManifestResourceNames())}");
            }
            using var reader = new StreamReader(stream);
            var sdl = reader.ReadToEnd();
            var schema = Schema.For(sdl, builder =>
            {
                builder.ServiceProvider = services;
                builder.Types.Include<MutationResolvers>();
                builder.Types.Include<QueryResolvers>();
                builder.Types.Include<SubscriptionResolvers>();

                var accessor = new SingleMethodAccessor(typeof(SubscriptionResolvers).GetMethod(nameof(SubscriptionResolvers.WatchCharacterStream)));
                var subscriber = new EventStreamResolver(accessor, services);
                var subField = builder.Types.For("Subscription").FieldFor("watchCharacter");
                subField.Subscriber = subscriber;
                subField.Resolver = new SourceFieldResolver<GameCharacter>();
            });
            
            return schema;
        }
    }

    class SourceFieldResolver<T> : IFieldResolver<T>
    {
        public T? Resolve(IResolveFieldContext context) => (T)context.Source;

        object? IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }

    internal class SingleMethodAccessor : IAccessor
    {
        public SingleMethodAccessor(MethodInfo method)
        {
            MethodInfo = method;
        }

        public string FieldName => MethodInfo.Name;

        public Type ReturnType => MethodInfo.ReturnType;

        public Type DeclaringType => MethodInfo.DeclaringType;

        public ParameterInfo[] Parameters => MethodInfo.GetParameters();

        public MethodInfo MethodInfo { get; }

        public IEnumerable<T> GetAttributes<T>() where T : Attribute => MethodInfo.GetCustomAttributes<T>();

        public object? GetValue(object target, object?[]? arguments)
        {
            try
            {
                return MethodInfo.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return null; // never executed, necessary only for intellisense
            }
        }
    }
}

