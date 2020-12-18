#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using Avatars;

namespace Sample
{
    public class CalculatorClassAvatar : Calculator, IAvatar
    {
        BehaviorPipeline pipeline = BehaviorPipelineFactory.Default.CreatePipeline<CalculatorClassAvatar>();

        public IList<IAvatarBehavior> Behaviors => pipeline.Behaviors;

        public override event EventHandler TurnedOn
        {
            add => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.TurnedOn += value; return m.CreateValueReturn(null, m.Arguments); }, value));
            remove => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.TurnedOn -= value; return m.CreateValueReturn(null, m.Arguments); }, value));
        }

        public override CalculatorMode Mode
        {
            get => pipeline.Execute<CalculatorMode>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Mode)));
            set => pipeline.Invoke(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.Mode = value; return m.CreateValueReturn(null, m.Arguments); }, value));
        }

        public override int? this[string name]
        {
            get => pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base[name]), name));
            set => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base[name] = value; return m.CreateValueReturn(null, m.Arguments); }, name, value));
        }

        public override bool IsOn => pipeline.Execute<bool>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.IsOn)));

        public override int Add(int x, int y) =>
            pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Add(x, y), m.Arguments), x, y));

        public override int Add(int x, int y, int z) =>
            pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Add(x, y, z), m.Arguments), x, y, z));

        public override bool TryAdd(ref int x, ref int y, out int z)
        {
            var method = MethodBase.GetCurrentMethod();
            z = default;

            var result = pipeline.Invoke(new MethodInvocation(this, method,
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
                }, x, y, z), true);

            x = result.Outputs.GetNullable<int>("x");
            y = result.Outputs.GetNullable<int>("y");
            z = result.Outputs.GetNullable<int>("z");

            return (bool)result.ReturnValue!;
        }

        public override void TurnOn() =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.TurnOn(); return m.CreateValueReturn(null); }));

        public override void Store(string name, int value) =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.Store(name, value); return m.CreateValueReturn(null, m.Arguments); }, name, value));

        public override int? Recall(string name) =>
            pipeline.Execute<int?>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Recall(name), m.Arguments), name));

        public override void Clear(string name) =>
            pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => { base.Clear(name); return m.CreateValueReturn(null, m.Arguments); }, name));

        public override ICalculatorMemory Memory
        {
            get => pipeline.Execute<ICalculatorMemory>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Memory)));
        }
    }
}