using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookService.Model.DTOs
{
    public class BookCreate
    {
        // public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Author { get; set; }
        public double? Price { get; set; }
        public string? Image { get; set; }
        public string? Category { get; set; }
        public int PageNumber { get; set; }
        public int TotalOfBook { get; set; }
    }
}