using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.Model;
using AuthService.Model.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

app.MapPost("/register", async (UserRegister userDto, AuthDbContext db) =>
{
    var newUser = new User
    {
        Name = userDto.Name,
        Email=userDto.Email,
        Password=userDto.Password,
        Role=userDto.Role
    };
    await db.Users.AddAsync(newUser);
    await db.SaveChangesAsync();
    return Results.Created("/login", "User Created Successfully!");
});

app.MapPost("/login", async (UserLogin userLogin, AuthDbContext db) =>
{
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Email.Equals(userLogin.Email) && u.Password.Equals(userLogin.Password));

    if (user == null) return Results.NotFound("The username or password is not correct!");

    var secretKey = builder.Configuration["Jwt:Key"];
    if (secretKey == null) return Results.StatusCode(500);

    var claims = new[]{
        new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
        new Claim(ClaimTypes.Email,user.Email),
        new Claim(ClaimTypes.GivenName,user.Name),
        new Claim(ClaimTypes.Surname,user.Name),
        new Claim(ClaimTypes.Role,user.Role),
    };
    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256));

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);
});

app.Run();
