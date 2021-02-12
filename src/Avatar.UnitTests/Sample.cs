using System;
using Avatars.Sample;
using Xunit;

namespace Avatars.UnitTests
{
    public class Sample
    {
        public void CanUseManualAvatar()
        {
            var avatar = new CalculatorAvatar();
            var recorder = new RecordingBehavior();
            avatar.AddBehavior(recorder);

            var isOn = false;
            avatar.TurnedOn += (_, __) => isOn = true;

            avatar.TurnOn();

            Assert.True(isOn);
            Assert.Equal(3, avatar.Add(1, 2));
            Assert.Equal(default, avatar.Mode);

            avatar.Store("balance", 100);
            Assert.Equal(100, avatar.Recall("balance"));
            Assert.Equal(100, avatar["balance"]);

            avatar.Store("mem1", 0);
            Assert.Equal(0, avatar.Recall("mem1"));
            Assert.Equal(0, avatar["mem1"]);

            var x = 1;
            var y = 2;
            int? z = 0;
            Assert.True(avatar.TryAdd(ref x, ref y, out z));
            Assert.Equal(3, z);

            Console.WriteLine(recorder.ToString());
        }
    }
}