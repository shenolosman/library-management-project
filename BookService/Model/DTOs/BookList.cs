using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookService.Model.DTOs
{
    public class BookList
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Author { get; set; }
        public double? Price { get; set; }
        public string? Image { get; set; }
        public string? Category { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PageNumber { get; set; }
        public int TotalOfBook { get; set; }
        public bool IsAvailable { get; set; }
    }
}