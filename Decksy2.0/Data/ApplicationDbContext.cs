using Decksy2._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Decksy2._0.Data;

    public class ApplicationDbContext : IdentityDbContext 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base (options)
        {

            
        }

        public DbSet<FlashCardModel> Flashcards { get; set; }

        public DbSet<DecksyModel> Decks { get; set; }

    }
