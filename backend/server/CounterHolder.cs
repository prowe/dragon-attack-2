namespace DragonAttack
{
    public class CounterHolder
    {
        public event EventHandler<int>? OnCounterChanged;

        private int counter = 0;
        public int Counter 
        { 
            get
            {
                return counter;
            }
            set
            {
                counter++;
                OnCounterChanged?.Invoke(this, counter);
            }
        }
    }
}