using System.Runtime.CompilerServices;
using System.Text;
using BookService.Data;
using BookService.Model;
using BookService.Model.DTOs;
using BookService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BookDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
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
});
builder.Services.AddGrpc();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookDbContext>();
    db.Database.Migrate();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<LibraryService>();

app.MapPost("/book", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (BookCreate bookDto, BookDbContext ctx) =>
{
    var createdBook = new Book
    {
        Author = bookDto.Author,
        Category = bookDto.Category,
        Image = bookDto.Image,
        Name = bookDto.Name,
        PageNumber = bookDto.PageNumber,
        Price = bookDto.Price,
        TotalOfBook = bookDto.TotalOfBook
    };

    await ctx.Books.AddAsync(createdBook);
    await ctx.SaveChangesAsync();

    return Results.Created($"/book/{createdBook.Name}", $"{createdBook.Name} created!");
});

app.MapPut("/book/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (Guid id, BookUpdate bookDto, BookDbContext ctx) =>
{
    if (bookDto.TotalOfBook < 0) return Results.BadRequest("Book amount can't be negative!");

    var book = await ctx.Books.FindAsync(id);

    if (book == null) return Results.NotFound("Book is not found!");

    book.Name = bookDto.Name;
    book.Author = bookDto.Author;
    book.Category = bookDto.Category;
    book.Image = bookDto.Image;
    book.IsAvailable = bookDto.IsAvailable;
    book.PageNumber = bookDto.PageNumber;
    book.Price = book.Price;
    book.UpdatedDate = DateTime.Now;

    await ctx.SaveChangesAsync();

    return Results.Ok("Book updated!");

});


app.MapDelete("/book/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")] async (Guid id, BookDbContext ctx) =>
{
    var book = await ctx.Books.FindAsync(id);

    if (book == null) return Results.NotFound("Couldn't find the book!");
    ctx.Books.Remove(book);

    await ctx.SaveChangesAsync();
    return Results.Ok($"{book.Name} was removed successfully!");
});

app.MapGet("/book/{id}", async (Guid id, BookDbContext ctx) =>
{
    var book = await ctx.Books.FindAsync(id);
    if (book == null) return Results.NotFound("Couldn't find the book!");

    var getBook = new BookList
    {
        Id = book.Id,
        Name = book.Name,
        Author = book.Author,
        Category = book.Category,
        Image = book.Image,
        PageNumber = book.PageNumber,
        Price = book.Price,
        TotalOfBook = book.TotalOfBook,
        CreatedDate = book.CreatedDate,
        UpdatedDate = book.UpdatedDate,
        IsAvailable = book.IsAvailable
    };

    return Results.Ok(getBook);
});

app.MapGet("/books", async (BookDbContext ctx) =>
{
    //here should return booklist dto
    var bookList=await ctx.Books.ToListAsync();
    var getBookList=new List<BookList>();
    foreach (var item in bookList)
    {
        var books=new BookList();
        books.Id=item.Id;
        books.Name=item.Name;
        books.Author=item.Author;
        books.Category=item.Category;
        books.CreatedDate=item.CreatedDate;
        books.UpdatedDate=item.UpdatedDate;
        books.Image=item.Image;
        books.IsAvailable=item.IsAvailable;
        books.PageNumber=item.PageNumber;
        books.Price=item.Price;
        books.TotalOfBook=item.TotalOfBook;
        
        getBookList.Add(books);
    }
    return getBookList;
});

app.Run();
