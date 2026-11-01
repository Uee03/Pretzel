namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Needs a runtime value (its name) and a service (the log), so it is created through a factory.</summary>
    public sealed class Enemy
    {
        private readonly ISampleLog _log;

        public Enemy(string name, int health, ISampleLog log)
        {
            Name = name;
            Health = health;
            _log = log;
        }

        public string Name { get; }

        public int Health { get; private set; }

        public void Hit(int damage)
        {
            Health -= damage;
            _log.Write("07 Factories", $"{Name} took {damage} damage, {Health} left");
        }
    }
}
