using System.ComponentModel.DataAnnotations;

namespace dotnet_example.Models  
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}