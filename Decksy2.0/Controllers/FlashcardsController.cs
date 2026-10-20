using System.Security.Claims;
using Decksy2._0.Data;
using Decksy2._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Decksy2._0.Controllers
{
    [Authorize]
    public class FlashcardsController : Controller
    {
        private const int MaxNameLength = 100;
        private const int MaxTextLength = 500;

        private readonly ApplicationDbContext _context;

        public FlashcardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private Task<DecksyModel?> FindDeckAsync(int id, bool withCards = false)
        {
            var query = _context.Decks.Where(d => d.Id == id && d.UserId == UserId);
            if (withCards)
            {
                query = query.Include(d => d.Flashcards);
            }
            return query.FirstOrDefaultAsync();
        }

        private static string? Clean(string? value, int maxLength)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return value.Length > maxLength ? value[..maxLength] : value;
        }

        // GET /Flashcards
        public async Task<IActionResult> Index()
        {
            var decks = await _context.Decks
                .Where(d => d.UserId == UserId)
                .OrderBy(d => d.Name)
                .Select(d => new DeckSummary(d.Id, d.Name, d.Flashcards.Count))
                .ToListAsync();

            return View(decks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeck(string? name)
        {
            name = Clean(name, MaxNameLength);
            if (name == null)
            {
                TempData["Error"] = "Deck name is required.";
                return RedirectToAction(nameof(Index));
            }

            var deck = new DecksyModel { Name = name, UserId = UserId };
            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Deck), new { id = deck.Id });
        }

        // GET /Flashcards/Deck/5
        public async Task<IActionResult> Deck(int id)
        {
            var deck = await FindDeckAsync(id, withCards: true);
            if (deck == null)
            {
                return NotFound();
            }

            deck.Flashcards = deck.Flashcards.OrderBy(c => c.Id).ToList();
            return View(deck);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameDeck(int id, string? name)
        {
            var deck = await FindDeckAsync(id);
            if (deck == null)
            {
                return NotFound();
            }

            name = Clean(name, MaxNameLength);
            if (name == null)
            {
                TempData["Error"] = "Deck name is required.";
            }
            else
            {
                deck.Name = name;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Deck), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDeck(int id)
        {
            var deck = await FindDeckAsync(id);
            if (deck == null)
            {
                return NotFound();
            }

            _context.Decks.Remove(deck);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Deck \"{deck.Name}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCard(int deckId, string? word, string? definition)
        {
            var deck = await FindDeckAsync(deckId);
            if (deck == null)
            {
                return NotFound();
            }

            word = Clean(word, MaxTextLength);
            definition = Clean(definition, MaxTextLength);
            if (word == null || definition == null)
            {
                TempData["Error"] = "Both the word and the definition are required.";
                return RedirectToAction(nameof(Deck), new { id = deckId });
            }

            _context.Flashcards.Add(new FlashCardModel { DeckId = deckId, Word = word, Definition = definition });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Deck), new { id = deckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCard(int id, string? word, string? definition)
        {
            var card = await _context.Flashcards
                .FirstOrDefaultAsync(c => c.Id == id && c.Deck.UserId == UserId);
            if (card == null)
            {
                return NotFound();
            }

            word = Clean(word, MaxTextLength);
            definition = Clean(definition, MaxTextLength);
            if (word == null || definition == null)
            {
                TempData["Error"] = "Both the word and the definition are required.";
            }
            else
            {
                card.Word = word;
                card.Definition = definition;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Deck), new { id = card.DeckId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _context.Flashcards
                .FirstOrDefaultAsync(c => c.Id == id && c.Deck.UserId == UserId);
            if (card == null)
            {
                return NotFound();
            }

            _context.Flashcards.Remove(card);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Deck), new { id = card.DeckId });
        }

        // GET /Flashcards/Study/5
        public async Task<IActionResult> Study(int id)
        {
            var deck = await FindDeckAsync(id, withCards: true);
            if (deck == null)
            {
                return NotFound();
            }
            if (deck.Flashcards.Count == 0)
            {
                TempData["Error"] = "Add some cards before you start studying.";
                return RedirectToAction(nameof(Deck), new { id });
            }

            deck.Flashcards = deck.Flashcards.OrderBy(c => c.Id).ToList();
            return View(deck);
        }

        // GET /Flashcards/Test/5
        public async Task<IActionResult> Test(int id)
        {
            var deck = await FindDeckAsync(id, withCards: true);
            if (deck == null)
            {
                return NotFound();
            }
            if (deck.Flashcards.Count == 0)
            {
                TempData["Error"] = "Add some cards before you start a test.";
                return RedirectToAction(nameof(Deck), new { id });
            }

            deck.Flashcards = deck.Flashcards.OrderBy(c => c.Id).ToList();
            return View(deck);
        }
    }

    public record DeckSummary(int Id, string Name, int CardCount);
}
