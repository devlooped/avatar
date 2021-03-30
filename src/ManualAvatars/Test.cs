using System;
//using Xunit;

namespace Sample
{
    public class Test
    {
#if NETSTANDARD2_1

        [Fact]
        public void Run()
        {
            IDefault @default = new Implementation();

            Assert.Equal(5, @default.Value);
        }


        public interface IDefault
        {
            void Do();
            int Value => 5;
        }

        public class Implementation : IDefault
        {
            public void Do() => throw new NotImplementedException();

            public int Value => Default.Instance.Value;
        }

        public class Default : IDefault
        {
            public static IDefault Instance { get; } = new Default();

            public void Do() => throw new NotImplementedException();
        }

#endif
    }
}
