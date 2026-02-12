using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

record UserLogin(string Username, string Password);

public static class LoginEndpoints
{
    public static void MapLoginEndpoints(this WebApplication app)
    {
        var jwtSettings = app.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        app.MapPost("/api/login", (HttpContext context, UserLogin login) =>
        {
            if (login.Username != "admin" || login.Password != "password")
                return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(jwtSettings["ExpireMinutes"]!)
                ),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Set httpOnly secure cookie for browser
            context.Response.Cookies.Append("access_token", tokenString,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(
                        double.Parse(jwtSettings["ExpireMinutes"]!)
                    )
                });

            // Also return token for API consumers
            return Results.Ok(new { token = tokenString });
        });

        // for cookie based auth (in the browser) only
        app.MapPost("/api/logout", (HttpContext context) =>
        {
            context.Response.Cookies.Delete("access_token");
            return Results.Ok();
        });
    }
}