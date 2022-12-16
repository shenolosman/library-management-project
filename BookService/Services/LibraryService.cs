using Grpc.Core;
using BookService.Protos;
using BookService.Data;
using Google.Protobuf.WellKnownTypes;

namespace BookService.Services
{
    public class LibraryService : GetBookService.GetBookServiceBase
    {
        private readonly BookDbContext _db;
        public ILogger<LibraryService> Logger { get; }
        public LibraryService(BookDbContext db, ILogger<LibraryService> logger)
        {
            this.Logger = logger;
            _db = db;
        }

        public override async Task<BookResponse> GetBook(BookRequest request, ServerCallContext serverCallContext)
        {
            var response = new BookResponse();
            var idGuid = new Guid(request.BookId);
            var book = await _db.Books.FindAsync(idGuid);
            if (book != null)
            {
                response.Id = book.Id.ToString();
                response.Name = book.Name;
                response.IsAvailable = book.IsAvailable;
                response.Author = book.Author;
                response.Price = (double)book.Price;
                response.Image = book.Image;
                response.Category = book.Category;
                // response.CreatedDate = Timestamp.FromDateTime(book.CreatedDate);
                response.PageNumber = book.PageNumber;
                response.TotalOfBook = book.TotalOfBook;
                response.IsAvailable = book.IsAvailable;
            }
            return await Task.FromResult(response);
        }
    }
}