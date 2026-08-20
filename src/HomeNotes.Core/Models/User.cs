using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HomeNotes.Core.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        public string Login { get; set; } = string.Empty;
         
        public string HashPassword { get; set; } = string.Empty;

        private DateTime _createAt = DateTime.UtcNow;

        public DateTime CreateAt
        {
            get => _createAt;
            set => _createAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        public ICollection<Notes> Notes { get; set; } = new List<Notes>();
    }
}
