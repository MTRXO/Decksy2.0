namespace Decksy2._0.Models
{
    public class DecksyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public List<FlashCardModel> Flashcards { get; set; } = new List<FlashCardModel>();
    }
}
