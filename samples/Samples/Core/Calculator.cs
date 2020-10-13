using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples
{
    class Calculator : ICalculator
    {
        readonly Dictionary<string, int> memory = new();

        public event EventHandler? TurnedOn;
        public event EventHandler? TurnedOff;

        public int? this[string name]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsOn { get; private set; }

        public CalculatorMode Mode { get; set; }

        public int Add(int x, int y) => x + y;

        public int Add(int x, int y, int z) => x + y + z;

        public void Clear(string name) => memory.Remove(name);

        public int? Recall(string name) => memory.TryGetValue(name, out var value) ? value : null;

        public void Store(string name, int value) => memory[name] = value;

        public bool TryAdd(ref int x, ref int y, out int z)
        {
            z = x + y;
            return true;
        }

        public void TurnOn()
        {
            IsOn = true;
            TurnedOn?.Invoke(this, EventArgs.Empty);
        }

        public void TurnOff()
        {
            IsOn = false;
            TurnedOn?.Invoke(this, EventArgs.Empty);
        }
    }
}
