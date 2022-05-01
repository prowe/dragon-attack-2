namespace DragonAttack
{
    public class Ability
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        public DiceSpecification Dice { get; set; }
        public AbilityEffect Effect { get; set; }
    }

    public enum AbilityEffect
    {
        Damage
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
    }
}