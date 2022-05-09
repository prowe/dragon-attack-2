namespace DragonAttack
{
    public class Ability
    {
        public Guid Id { get; set; }
        [GraphQLType("String!")]
        public String Name { get; set; }
        internal DiceSpecification Dice { get; set; } = new DiceSpecification();
        internal AbilityEffect Effect { get; set; }
        internal int MaxTargets { get; set; } = 1;
        public TimeSpan Cooldown { get; set; } = TimeSpan.Zero;
    }

    public enum AbilityEffect
    {
        Damage,
        Heal
    }

    public class DiceSpecification
    {
        public int Sides { get; set; } = 6;
        public int Rolls { get; set; } = 1;
        public int Constant { get; set; } = 0;

        public int Roll()
        {
            return Enumerable.Range(0, Rolls)
                .Select(_ => Random.Shared.Next(Sides) + 1)
                .Sum() + Constant;
        }

        public int Max => Sides * Rolls + Constant;
        public int Min => Rolls + Constant;
        public float Average => ((float)Min + (float)Max)  / 2;
    }
}