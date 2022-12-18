using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using LoanService.Model;
using Grpc.Net.Client;
using LoanService.Protos;
using LoanService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoanContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
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

builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapPost("/", () => { return Results.Ok("Hello Loan"); });
app.MapPost("/{bookId}", async (string bookId, LoanContext ctx, HttpContext httpContext) =>
{
    using var channel = GrpcChannel.ForAddress("https://inventorycontainer--1zc5jvi.kindforest-a7062dfd.eastus.azurecontainerapps.io");
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
    var userId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
    if (userId == null)
    {
        return Results.BadRequest("Bad token");
    }

    var loan = new Loan();
    loan.Id = Guid.NewGuid();
    loan.UserId = userId;
    loan.LoanedDate = DateTime.UtcNow;
    loan.BookAuthor = book.Author;
    loan.BookCategory = book.Category;
    loan.TotalOfBook = (book.TotalOfBook) - 1;
    if (loan.TotalOfBook == 0)
    {
        loan.Available = false;
        return Results.BadRequest("No Book left in library!");
    };
    loan.Available = book.IsAvailable;

    await ctx.Loans.AddAsync(loan);
    await ctx.SaveChangesAsync();

    return Results.Created($"/{loan.Id}", "Book has been loaned");
});

app.Run();