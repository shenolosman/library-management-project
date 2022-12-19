namespace LoanService.Model
{
    public class Loan
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public DateTime? LoanedDate { get; set; }
        public string? BookAuthor { get; set; }
        public string? BookCategory { get; set; }
        public int? TotalOfBook { get; set; }
        public bool? Available { get; set; }
    }
}