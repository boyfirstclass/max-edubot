using EduMaxBot.Data;
using EduMaxBot.Integrations;
using EduMaxBot.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MaxApiOptions>(builder.Configuration.GetSection("MaxApi"));

var cs = builder.Configuration.GetConnectionString("Postgres")
         ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
         ?? "Host=localhost;Port=5432;Database=edumaxbot;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(cs);
});

builder.Services.AddHttpClient<MaxApiClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MaxApiOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl ?? "https://platform-api.max.ru/");
    http.DefaultRequestHeaders.Add("Authorization", opt.Token ?? "");
});

builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<ReviewService>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Run();
