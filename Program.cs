using System.Reflection;
using Auth0.AspNetCore.Authentication;
using Chefster;
using Chefster.Context;
using Chefster.Services;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

try
{
    var path = Path.Combine(builder.Environment.ContentRootPath, ".env");
    DotEnv.Load(path);
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading .env file: {ex.Message}");
}

var domain = Environment.GetEnvironmentVariable("AUTH_DOMAIN");
var clientId = Environment.GetEnvironmentVariable("AUTH_CLIENT_ID");
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    if (domain != null && clientId != null)
    {
        options.Domain = domain;
        options.ClientId = clientId;
        options.Scope = "openid profile email";
    }
});

var connString = Environment.GetEnvironmentVariable("SQL_CONN_STR");
builder.Services.AddDbContext<ChefsterDbContext>(options =>
{
    options.UseSqlServer(connString);
    options.UseLoggerFactory(
        LoggerFactory.Create(builder => builder.AddFilter((category, level) => false))
    );
    // options.UseInMemoryDatabase("TestDB");
});

//GlobalConfiguration.Configuration.UseSqlServerStorage("connection String");

builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<ConsiderationsService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<GordonService>();
builder.Services.AddScoped<ViewToStringService>();
builder.Services.AddScoped<PreviousRecipesService>();
builder.Services.AddScoped<UpdateProfileService>();
builder.Services.AddControllers();

builder.Services.AddControllersWithViews();

// Hangfire stuff
var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONN");
var mongoUrlBuilder = new MongoUrlBuilder(mongoConnection);
var mongoClient = new MongoClient(mongoConnection);
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
            // .UseSqlServerStorage(connString);
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

builder.Services.AddHangfireServer();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var isProd = Environment.GetEnvironmentVariable("IS_PROD");
if (isProd == "false")
{
    Console.WriteLine("WE ARE IN DEVELOPMENT!");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chefster Backend");
    });
}

app.UseHangfireDashboard();
app.MapHangfireDashboard();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");

app.Run();
