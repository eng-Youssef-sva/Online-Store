using System;
using System.Collections.Generic;
using System.Text;

namespace Online_Store.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public int Rating { get; set; } // 1..5
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
