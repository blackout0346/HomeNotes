using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Net.Mail;
using HomeNotes.Core.Models;
namespace HomeNotes.Infrastucture.Data
{
    public class AppDbContext : DbContext
    {

        public DbSet<Attachments> Attachments {  get; set; }
        public DbSet<Notes> Notes { get; set; }
        public DbSet<User> User { get; set;  }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notes>(entity=>
            {
                entity.HasOne(b => b.User).WithMany(b=>b.Notes).HasForeignKey(b=>b.UserId);
            });
            modelBuilder.Entity<Attachments>(entity =>
            {
                entity.HasOne(b => b.Notes).WithMany(b => b.Attachments).HasForeignKey(b=>b.NotesId);
            });

        }
    }

}
