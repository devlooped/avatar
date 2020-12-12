#pragma warning disable CS0436
using System;
using Sample;
using Xunit;

namespace Avatars.Scenarios.ClassBaseAbstractType
{
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<CalculatorBase>();

            Assert.Throws<NotImplementedException>(() => avatar.Mode = CalculatorMode.Scientific);
            Assert.Throws<NotImplementedException>(() => avatar.Mode);
            Assert.Throws<NotImplementedException>(() => avatar.TurnOn());
        }
    }
}
