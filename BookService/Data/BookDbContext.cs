using BookService.Model;
using Microsoft.EntityFrameworkCore;

namespace BookService.Data
{
    public class BookDbContext:DbContext
    {
        public BookDbContext(DbContextOptions<BookDbContext> opt):base(opt) { }
        public DbSet<Book> Books { get; set; }
    }
}