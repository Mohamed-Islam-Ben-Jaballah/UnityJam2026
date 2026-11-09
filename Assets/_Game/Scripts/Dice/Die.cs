using System;

namespace DiceDrive.DiceSystem
{
    public sealed class Die
    {
        public int Sides { get; }

        public Die(int sides)
        {
            if (sides < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(sides));
            }

            Sides = sides;
        }

        public int Roll(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return random.Next(1, Sides + 1);
        }
    }
}
