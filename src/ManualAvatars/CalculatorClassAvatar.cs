#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using Avatars;

namespace Sample
{
    public class CalculatorClassAvatar : Calculator, IAvatar
    {
        BehaviorPipeline pipeline = new BehaviorPipeline();

        public IList<IAvatarBehavior> Behaviors => pipeline.Behaviors;

        public override event EventHandler TurnedOn
        {
            add => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), value), (m, n) => { base.TurnedOn += value; return m.CreateValueReturn(null, m.Arguments); });
            remove => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), value), (m, n) => { base.TurnedOn -= value; return m.CreateValueReturn(null, m.Arguments); });
        }

        public override CalculatorMode Mode
        {
            get => pipeline.Execute<CalculatorMode>(new MethodInvocation(this, MethodBase.GetCurrentMethod()), (m, n) => m.CreateValueReturn(base.Mode));
            set => pipeline.Invoke(new MethodInvocation(this, MethodBase.GetCurrentMethod(), value), (m, n) => { base.Mode = value; return m.CreateValueReturn(null, m.Arguments); });
        }

        public override int? this[string name]
        {
            get => pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name), (m, n) => m.CreateValueReturn(base[name]));
            set => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name, value), (m, n) => { base[name] = value; return m.CreateValueReturn(null, m.Arguments); });
        }

        public override bool IsOn => pipeline.Execute<bool>(new MethodInvocation(this, MethodBase.GetCurrentMethod()), (m, n) => m.CreateValueReturn(base.IsOn));

        public override int Add(int x, int y) =>
            pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), x, y), (m, n) => m.CreateValueReturn(base.Add(x, y), m.Arguments));

        public override int Add(int x, int y, int z) =>
            pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), x, y, z), (m, n) => m.CreateValueReturn(base.Add(x, y, z), m.Arguments));

        public override bool TryAdd(ref int x, ref int y, out int z)
        {
            var method = MethodBase.GetCurrentMethod();
            z = default;

            var result = pipeline.Invoke(new MethodInvocation(this, method, x, y, z),
                (m, n) => 
                {
                    var local_x = m.Arguments.Get<int>("x");
                    var local_y = m.Arguments.Get<int>("y");
                    var local_z = m.Arguments.Get<int>("z");
                    return m.CreateValueReturn(base.TryAdd(ref local_x, ref local_y, out local_z), 
                        new ArgumentCollection(method.GetParameters())
                        {
                            { "x", local_x },
                            { "y", local_y },
                            { "z", local_z },
                        }); 
                }, true);

            x = result.Outputs.GetNullable<int>("x");
            y = result.Outputs.GetNullable<int>("y");
            z = result.Outputs.GetNullable<int>("z");

            return (bool)result.ReturnValue!;
        }

        public override void TurnOn() =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod()), (m, n) => { base.TurnOn(); return m.CreateValueReturn(null); });

        public override void Store(string name, int value) =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name, value), (m, n) => { base.Store(name, value); return m.CreateValueReturn(null, m.Arguments); });

        public override int? Recall(string name) =>
            pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name), (m, n) => m.CreateValueReturn(base.Recall(name), m.Arguments));

        public override void Clear(string name) =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), name), (m, n) => { base.Clear(name); return m.CreateValueReturn(null, m.Arguments); });

        public override ICalculatorMemory Memory
        {
            get => pipeline.Execute<ICalculatorMemory>(new MethodInvocation(this, MethodBase.GetCurrentMethod()), (m, n) => m.CreateValueReturn(base.Memory))!;
        }
    }
}