using System;

namespace Samples
{
    public interface ICalculator
    {
        event EventHandler? TurnedOn;

        event EventHandler? TurnedOff;

        bool IsOn { get; }

        CalculatorMode Mode { get; set; }

        int Add(int x, int y);

        int Add(int x, int y, int z);

        bool TryAdd(ref int x, ref int y, out int z);

        void TurnOn();

        void TurnOff();

        int? this[string name] { get; set; }

        void Store(string name, int value);

        int? Recall(string name);

        void Clear(string name);
    }
}