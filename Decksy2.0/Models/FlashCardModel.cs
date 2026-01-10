namespace Decksy2._0.Models
{
    public class FlashCardModel
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public string Definition { get; set; }
        public int DeckId { get; set; }
        public DecksyModel Deck { get; set; }
    }
}
