using GraphQL;

namespace DragonAttack
{
    public class GameCharacter
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int HealthPercent { get; set; }
    }

    public class IGameCharacterGrain
    {
        
    }
}