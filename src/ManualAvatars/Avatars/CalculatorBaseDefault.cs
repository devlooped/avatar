using System;
using Sample;

namespace Avatars.Sample
{
    public class ICalculatorDefault : ICalculator, IDisposable
    {
        public int? this[string name] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsOn => throw new NotImplementedException();

        public CalculatorMode Mode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ICalculatorMemory Memory => throw new NotImplementedException();

        public event EventHandler TurnedOn
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public int Add(int x, int y) => throw new NotImplementedException();
        public int Add(int x, int y, int z) => throw new NotImplementedException();
        public void Clear(string name) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
        public int? Recall(string name) => throw new NotImplementedException();
        public void Store(string name, int value) => throw new NotImplementedException();
        public bool TryAdd(ref int x, ref int y, out int? z) => throw new NotImplementedException();
        public void TurnOn() => throw new NotImplementedException();
    }

    public class CalculatorBaseDefault : CalculatorBase
    {
        static readonly CalculatorBaseDefault instance = new CalculatorBaseDefault();

        CalculatorBaseDefault() { }

        public static CalculatorBaseDefault Instance { get; } = new CalculatorBaseDefault();

        public override int? this[string name] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override bool IsOn => throw new NotImplementedException();

        public override CalculatorMode Mode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override ICalculatorMemory Memory => throw new NotImplementedException();

        public override event EventHandler TurnedOn
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public override int Add(int x, int y) => throw new NotImplementedException();
        public override int Add(int x, int y, int z) => throw new NotImplementedException();
        public override void Clear(string name) => throw new NotImplementedException();
        public override int? Recall(string name) => throw new NotImplementedException();
        public override void Store(string name, int value) => throw new NotImplementedException();
        public override bool TryAdd(ref int x, ref int y, out int? z) => throw new NotImplementedException();
        public override void TurnOn() => throw new NotImplementedException();
    }
}
