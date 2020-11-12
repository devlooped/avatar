using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Avatars.UnitTests
{
    public class RefReturns
    {
        [Fact]
        public void CanReturnRef()
        {
            INumbers avatar = new NumbersAvatar();


            Ref<int> value = 25;

            avatar.AddBehavior((invocation, next) => invocation.CreateValueReturn(value, invocation.Arguments.ToArray()));

            var index = 0;
            ref int v = ref avatar.NumberAt(ref index, out var count);

            v = 42;

            Assert.Equal(42, value.Value);
            Assert.True(42 == value);
        }

        public interface INumbers
        {
            ref int NumberAt(ref int index, out int count);
        }

        public class NumbersAvatar : INumbers, IAvatar
        {
            BehaviorPipeline pipeline = new();

            public IList<IAvatarBehavior> Behaviors => pipeline.Behaviors;

            public ref int NumberAt(ref int index, out int count)
            {
                count = default;
                var local_index = index;

                var returns = pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), local_index, count));
                index = returns.Outputs.GetNullable<int>("index");
                count = returns.Outputs.GetNullable<int>("count");

                return ref ((Ref<int>)returns.ReturnValue).Value;
            }
        }
    }
}
