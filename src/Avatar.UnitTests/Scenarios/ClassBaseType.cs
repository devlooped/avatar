#pragma warning disable CS0436
using System;
using System.Collections.Generic;
using Xunit;

namespace Avatars.Scenarios.ClassBaseType
{
    public abstract class BaseType
    {
        Dictionary<(int, string), string> values = new();

        public abstract event EventHandler? AbstractBase;
        public virtual event EventHandler? TurnedOn;

        public bool IsOn { get; private set; }

        public virtual string this[int index, string key]
        {
            get => values.TryGetValue((index, key), out var value) ? value : "";
            set => values[(index, key)] = value;
        }

        public virtual PlatformID Platform { get; set; } = PlatformID.Win32NT;

        public virtual bool TryAdd(int x, int y, ref int mem, out int result)
        {
            result = x + y;
            return true;
        }

        public virtual void TurnOn()
        {
            TurnedOn?.Invoke(this, EventArgs.Empty);
            IsOn = true;
        }
    }

    public class Test : IRunnable
    {
        // Run this particular scenario using the TD.NET ad-hoc runner.
        public void RunScenario() => new UnitTests.Scenarios().Run(ThisAssembly.Constants.Scenarios.ClassBaseType);

        public void Run()
        {
            var avatar = Avatar.Of<BaseType>();

            avatar.Platform = PlatformID.MacOSX;

            Assert.Equal(PlatformID.MacOSX, avatar.Platform);

            Assert.False(avatar.IsOn);

            var on = false;
            avatar.TurnedOn += (_, _) => on = true;
            avatar.TurnOn();
            Assert.True(avatar.IsOn);
            Assert.True(on);

            var x = 5;
            var y = 10;
            var mem = 42;
            Assert.True(avatar.TryAdd(x, y, ref mem, out var z));
            Assert.Equal(15, z);

            avatar[0, "foo"] = "bar";

            Assert.Equal("bar", avatar[0, "foo"]);
        }
    }
}
