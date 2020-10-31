#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using Avatars;

namespace Sample
{
    public class CalculatorInterfaceAvatar : ICalculator, IDisposable, IAvatar
    {
        BehaviorPipeline pipeline = new BehaviorPipeline();

        IList<IAvatarBehavior> IAvatar.Behaviors => pipeline.Behaviors;

        public event EventHandler TurnedOn
        {
            add => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod()));
            remove => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod()));
        }

        public bool IsOn
        {
            get => pipeline.Execute<bool>(new MethodInvocation(this, MethodBase.GetCurrentMethod()));
        }

        public CalculatorMode Mode
        {
            get => pipeline.Execute<CalculatorMode>(new MethodInvocation(this, MethodBase.GetCurrentMethod()));
            set => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), value));
        }

        public int? this[string name]
        {
            get => pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name));
            set => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name, value));
        }   

        public int Add(int x, int y) => pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), x, y));

        public int Add(int x, int y, int z) => pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), x, y, z));

        public bool TryAdd(ref int x, ref int y, out int z)
        {
            z = default;
            var returns = pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), x, y, z));

            x = returns.Outputs.GetNullable<int>("x");
            y = returns.Outputs.GetNullable<int>("y");
            z = returns.Outputs.GetNullable<int>("z");

            return (bool)returns.ReturnValue!;
        }

        public void TurnOn() => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod()));

        public void Store(string name, int value) => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name, value));

        public int? Recall(string name) => pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name));

        public void Clear(string name) => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name));

        public void Dispose() => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod()));

        public ICalculatorMemory Memory
        {
            get => pipeline.Execute<ICalculatorMemory>(new MethodInvocation(this, MethodBase.GetCurrentMethod()))!;
        }
    }
}
