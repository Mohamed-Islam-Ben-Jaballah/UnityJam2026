namespace DiceDrive.Cards
{
    public sealed class CardInstance
    {
        private static int _nextId = 1;

        public int Id { get; }
        public CardData Data { get; }
        public int HandOrder { get; set; }

        public CardInstance(CardData data)
        {
            Data = data;
            Id = _nextId++;
        }
    }
}
