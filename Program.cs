var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<ActivityQueries>();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapActivityEndpoints();



app.Run();


