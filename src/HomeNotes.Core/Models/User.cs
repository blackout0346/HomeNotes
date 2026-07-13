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
         
        public string hashpassword { get; set; } = string.Empty;

        public DateTime CreateAt { get; set; } = DateTime.Now;
        public ICollection<Notes> Notes { get; set; } = new List<Notes>();
    }
}
