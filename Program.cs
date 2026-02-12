using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Load JWT settings
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    options.Events = new JwtBearerEvents
    {
        // read token from httpOnly cookie, for VueJS
        ////////////////////////////
        /// for example:
        /// 
        /// await axios.post("https://localhost:5001/login",
        ///   {
        ///     username: "admin",
        ///     password: "password"
        ///   },
        ///   {
        ///     withCredentials: true // 🔥 important
        ///   }
        /// );
        ///
        /// const response = await axios.get(
        ///   "https://localhost:5001/secure",
        ///   {
        ///     withCredentials: true
        ///   }
        /// );
        ///  
        OnMessageReceived = context =>
        {
            var cookieToken = context.Request.Cookies["access_token"];

            if (!string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<ActivityQueries>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapActivityEndpoints();
app.MapLoginEndpoints();

app.Run();


