using System;
using System.ComponentModel.DataAnnotations;

namespace BaiTHbuoi1.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Author { get; set; }

        public DateTime PublishDate { get; set; } = DateTime.Now;

        public bool IsPublished { get; set; } = true;
    }
}