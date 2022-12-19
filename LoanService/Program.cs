using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Grpc.Net.Client;
using System.Text;
using LoanService;
using LoanService.Protos;
using LoanService.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoanContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.SaveToken = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LoanContext>();
    ctx.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();


app.MapPost("loan/{bookId}", async (string bookId, LoanContext ctx, HttpContext http) =>
{
    var channel = GrpcChannel.ForAddress("http://192.168.10.151:6002");
    var client = new GetBookService.GetBookServiceClient(channel);

    var bookRequest = new BookRequest
    {
        BookId = bookId
    };

    var book = await client.GetBookAsync(bookRequest);

    if (book == null)
    {
        return Results.NotFound("Book was not found");
    }
    var userId = http.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

    if (userId == null)
    {
        return Results.BadRequest("Bad token!");
    }
    var loan = new Loan();
    loan.UserId = userId;
    loan.LoanedDate = DateTime.UtcNow;
    loan.BookAuthor = book.Author;
    loan.BookCategory = book.Category;
    loan.Available = book.IsAvailable;
    if (loan.TotalOfBook == null || loan.TotalOfBook == 0)
    {
        loan.TotalOfBook = book.TotalOfBook;
    }
    else
    {
        loan.TotalOfBook -= -1;
        if (loan.TotalOfBook == 0)
        {
            loan.Available = false;
            return Results.BadRequest("No Book left in library!");
        };
    }
    await ctx.Loans.AddAsync(loan);
    await ctx.SaveChangesAsync();

    return Results.Created($"loan/{loan.Id}", "Book has been loaned");
});
app.MapGet("/loan/getall", async (LoanContext ctx, HttpContext http) =>
{
    var userId = http.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

    if (userId == null) return Results.BadRequest("Bad token!");

    return Results.Ok(await ctx.Loans.ToListAsync());
});
app.Run();