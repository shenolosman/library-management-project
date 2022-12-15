namespace LoanService.Model
{
    public class Loan
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string? BookId { get; set; }
        public DateTime LoanedDate { get; set; } = DateTime.Now;
    }
}