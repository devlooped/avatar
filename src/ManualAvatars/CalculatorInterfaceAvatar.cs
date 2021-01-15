#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using Avatars;

namespace Sample
{
    public class CalculatorInterfaceAvatar : ICalculator, IDisposable, IAvatar
    {
        readonly BehaviorPipeline pipeline = new();

        IList<IAvatarBehavior> IAvatar.Behaviors => pipeline.Behaviors;

        public event EventHandler TurnedOn
        {
            add => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), value));
            remove => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), value));
        }

        public bool IsOn
        {
            get => pipeline.Execute<bool>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod()));
        }

        public CalculatorMode Mode
        {
            get => pipeline.Execute<CalculatorMode>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod()));
            set => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), value));
        }

        public int? this[string name]
        {
            get => pipeline.Execute<int?>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), name));
            set => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), name, value));
        }

        public int Add(int x, int y) => pipeline.Execute<int>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), x, y));

        public int Add(int x, int y, int z) => pipeline.Execute<int>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), x, y, z));

        public bool TryAdd(ref int x, ref int y, out int z)
        {
            z = default;
            var returns = pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), x, y, z));

            x = returns.Outputs.GetNullable<int>("x");
            y = returns.Outputs.GetNullable<int>("y");
            z = returns.Outputs.GetNullable<int>("z");

            return (bool)returns.ReturnValue!;
        }

        public void TurnOn() => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod()));

        public void Store(string name, int value) => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), name, value));

        public int? Recall(string name) => pipeline.Execute<int?>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), name));

        public void Clear(string name) => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod(), name));

        public void Dispose() => pipeline.Execute(MethodInvocation.Create(this, MethodBase.GetCurrentMethod()));

        public ICalculatorMemory Memory
        {
            get => pipeline.Execute<ICalculatorMemory>(MethodInvocation.Create(this, MethodBase.GetCurrentMethod()))!;
        }
    }
}
