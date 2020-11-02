using System;
using Sample;
using Xunit;

namespace Avatars.Scenarios.ClassBaseAbstractType
{
    public class Test : IRunnable
    {
        // Run this particular scenario using the TD.NET ad-hoc runner.
        public void RunScenario() => new UnitTests.Scenarios().Run(ThisAssembly.Constants.Scenarios.ClassBaseAbstractType);

        public void Run()
        {
            var avatar = Avatar.Of<CalculatorBase>();

            Assert.Throws<NotImplementedException>(() => avatar.Mode = CalculatorMode.Scientific);
            Assert.Throws<NotImplementedException>(() => avatar.Mode);
            Assert.Throws<NotImplementedException>(() => avatar.TurnOn());
        }
    }
}
