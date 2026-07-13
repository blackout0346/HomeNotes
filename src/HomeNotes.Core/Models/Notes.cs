using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HomeNotes.Core.Models
{
    public class Notes
    {
        [Key]
        public Guid Id { get; set; }
 
        public string Title { get; set; } = string.Empty;

        public string RelativePath { get; set; } = "";

        public Guid UserId { get; set; } 

        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public bool IsSynced { get; set; }
        public ICollection<Attachments> Attachments { get; set; } = [];
    }
}
