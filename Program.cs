using System.Reflection;
using Auth0.AspNetCore.Authentication;
using Chefster.Common;
using Chefster.Consumers;
using Chefster.Context;
using Chefster.Models;
using Chefster.Services;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Limit local db logging
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);

// load local dotnet user-secrets
builder.Configuration.AddUserSecrets("d70ac473-6c10-438a-a3ba-2a154bdf5946");

// load secrets from aws parameter store
builder.Configuration.AddSystemsManager(c =>
{
    c.Path = "/Chefster/Development";
    c.Optional = true;
    c.ReloadAfter = TimeSpan.FromMinutes(10);
});

Console.WriteLine("Builder's Environment: " + builder.Environment.EnvironmentName);

//setup auth0
builder
    .Services.AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = builder.Configuration["AUTH_DOMAIN"]!;
        options.ClientId = builder.Configuration["AUTH_CLIENT_ID"]!;
        options.ClientSecret = builder.Configuration["AUTH_CLIENT_SECRET"];
        options.Scope = "openid profile email offline_access";
    })
    .WithAccessToken(options =>
    {
        options.Audience = builder.Configuration["AUTH_AUDIENCE"];
        options.UseRefreshTokens = true;
    });

// Configure the login path
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
});

// set up Stripe
StripeConfiguration.ApiKey = builder.Environment.IsDevelopment()
    ? builder.Configuration["STRIPE_SECRET_DEV"]
    : builder.Configuration["STRIPE_SECRET_PROD"];

// set up db
builder.Services.AddDbContext<ChefsterDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite("Data Source=ChefsterTestDB.db");
    }
    else if (builder.Environment.IsProduction())
    {
        var endpoint = builder.Configuration["MYSQL_ENDPOINT"];
        var db = builder.Configuration["MYSQL_DB"];
        var username = builder.Configuration["MYSQL_USERNAME"];
        var password = builder.Configuration["MYSQL_PASSWORD"];
        options.UseMySql(
            $"Server={endpoint};Database={db};User={username};Password={password}",
            new MySqlServerVersion(new Version(8, 0, 35))
        );
        options.UseLoggerFactory(
            LoggerFactory.Create(builder => builder.AddFilter((category, level) => false))
        );
    }
});

/*
*
* Logging Setup
*
*/

builder.Services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(
    builder.Configuration["MONGO_LOG_CONN"]
));

// Add LoggingService as a singleton so it's available across the app
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    ServiceProviderFactory.ServiceProvider = sp;
    return new LoggingService(client, builder.Configuration);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("chefster-logs");
});

builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<LogModel>("logs");
});

builder.Services.AddHttpContextAccessor();

// add services
builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<ConsiderationsService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<GordonService>();
builder.Services.AddScoped<ViewToStringService>();
builder.Services.AddScoped<PreviousRecipesService>();
builder.Services.AddScoped<UpdateProfileService>();
builder.Services.AddScoped<HubSpotService>();
builder.Services.AddScoped<LetterQueueService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<JobRecordService>();
builder.Services.AddScoped<Chefster.Services.SubscriptionService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSingleton<StripeMessageConsumer>();
builder.Services.AddHostedService<StripeMessageConsumer>();
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

// add hangfire with mongo
var mongoUrlBuilder = new MongoUrlBuilder(builder.Configuration["MONGO_CONN"]);
var mongoClient = new MongoClient(builder.Configuration["MONGO_CONN"]);
var migrationOptions = new MongoMigrationOptions
{
    MigrationStrategy = new DropMongoMigrationStrategy(),
    BackupStrategy = new CollectionMongoBackupStrategy()
};

builder.Services.AddHangfire(
    (sp, configuration) =>
    {
        configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMongoStorage(
                mongoClient,
                "chefster-hangfire",
                new MongoStorageOptions
                {
                    MigrationOptions = migrationOptions,
                    CheckConnection = false
                }
            );
    }
);
if (builder.Environment.IsDevelopment())
{
    // handy when developing frontend stuff. Can cause wonkiness when server side rendering pages
    var mvc = builder.Services.AddRazorPages();
    mvc.AddRazorRuntimeCompilation();
    builder.Services.AddHangfireServer(options =>
        options.Queues = [builder.Configuration["QUEUE_NAME"]]
    );
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddHangfireServer();
}

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Chefster Backend", Version = "v1" });
    c.IncludeXmlComments(
        Path.Combine(
            AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
        ),
        true
    );
});

builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chefster Backend"));
}

app.UseHangfireDashboard();
app.MapHangfireDashboard();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
app.Run();
