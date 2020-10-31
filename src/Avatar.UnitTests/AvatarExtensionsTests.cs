using System;
using System.Collections.Generic;
using Xunit;

namespace Avatars.UnitTests
{
    public class AvatarExtensionsTests
    {
        [Fact]
        public void AddAnonymousBehavior()
        {
            IAvatar avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.AddBehavior(
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(AddBehavior));

            Assert.Same(avatar, actual);
            Assert.Single(avatar.Behaviors);
            Assert.Equal(nameof(AddBehavior), actual.Behaviors[0].ToString());
            Assert.True(actual.Behaviors[0].AppliesTo(null!));
            Assert.Equal("foo", (string?)actual.Behaviors[0].Execute(new MethodInvocation(this, method.Method), null!).ReturnValue);
        }

        [Fact]
        public void AddBehavior()
        {
            IAvatar avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.AddBehavior(new TestBehavior());

            Assert.Same(avatar, actual);
            Assert.Single(avatar.Behaviors);
            Assert.Equal(nameof(TestBehavior), actual.Behaviors[0].ToString());
            Assert.True(actual.Behaviors[0].AppliesTo(null!));
            Assert.Equal("test", (string?)actual.Behaviors[0].Execute(new MethodInvocation(this, method.Method), null!).ReturnValue);
        }

        [Fact]
        public void AddAnonymousBehaviorToObject()
        {
            object avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.AddBehavior(
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(AddBehavior)) as IAvatar;

            Assert.NotNull(actual);
            Assert.Same(avatar, actual);
            Assert.Single(actual!.Behaviors);
        }

        [Fact]
        public void AddAnonymousBehaviorToNonAvatarThrows()
        {
            object avatar = new object();
            Func<string?> method = ToString;

            Assert.Throws<ArgumentException>(() => avatar.AddBehavior(
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(AddBehavior)));
        }

        [Fact]
        public void AddBehaviorToNonAvatarThrows()
        {
            object avatar = new object();
            Func<string?> method = ToString;

            Assert.Throws<ArgumentException>(() => avatar.AddBehavior(new TestBehavior()));
        }

        [Fact]
        public void AddBehaviorToObject()
        {
            object avatar = new TestAvatar();

            var actual = avatar.AddBehavior(new TestBehavior()) as IAvatar;

            Assert.NotNull(actual);
            Assert.Same(avatar, actual);
            Assert.Single(actual!.Behaviors);
        }

        [Fact]
        public void InsertAnonymousBehavior()
        {
            IAvatar avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.InsertBehavior(0,
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(InsertAnonymousBehavior));

            Assert.Same(avatar, actual);
            Assert.Single(avatar.Behaviors);
            Assert.Equal(nameof(InsertAnonymousBehavior), actual.Behaviors[0].ToString());
            Assert.True(actual.Behaviors[0].AppliesTo(null!));
            Assert.Equal("foo", (string?)actual.Behaviors[0].Execute(new MethodInvocation(this, method.Method), null!).ReturnValue);
        }

        [Fact]
        public void InsertBehavior()
        {
            IAvatar avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.InsertBehavior(0, new TestBehavior());

            Assert.Same(avatar, actual);
            Assert.Single(avatar.Behaviors);
            Assert.Equal(nameof(TestBehavior), actual.Behaviors[0].ToString());
            Assert.True(actual.Behaviors[0].AppliesTo(null!));
            Assert.Equal("test", (string?)actual.Behaviors[0].Execute(new MethodInvocation(this, method.Method), null!).ReturnValue);
        }

        [Fact]
        public void InsertAnonymousBehaviorToObject()
        {
            object avatar = new TestAvatar();
            Func<string?> method = ToString;

            var actual = avatar.InsertBehavior(0,
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(InsertAnonymousBehaviorToObject)) as IAvatar;

            Assert.NotNull(actual);
            Assert.Same(avatar, actual);
            Assert.Single(actual!.Behaviors);
        }

        [Fact]
        public void InsertAnonymousBehaviorToNonAvatarThrows()
        {
            object avatar = new object();
            Func<string?> method = ToString;

            Assert.Throws<ArgumentException>(() => avatar.InsertBehavior(0,
                (m, n) => new MethodReturn(m, "foo", null!),
                m => true,
                nameof(InsertAnonymousBehaviorToNonAvatarThrows)));
        }

        [Fact]
        public void InsertBehaviorToNonAvatarThrows()
        {
            object avatar = new object();
            Func<string?> method = ToString;

            Assert.Throws<ArgumentException>(() => avatar.InsertBehavior(0, new TestBehavior()));
        }

        [Fact]
        public void InsertBehaviorToObject()
        {
            object avatar = new TestAvatar();

            var actual = avatar.InsertBehavior(0, new TestBehavior()) as IAvatar;

            Assert.NotNull(actual);
            Assert.Same(avatar, actual);
            Assert.Single(actual!.Behaviors);
        }

        class TestBehavior : IAvatarBehavior
        {
            public bool AppliesTo(IMethodInvocation invocation) => true;

            public IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next)
                => new MethodReturn(invocation, "test", Array.Empty<object>());

            public override string ToString() => nameof(TestBehavior);
        }

        class TestAvatar : IAvatar
        {
            public IList<IAvatarBehavior> Behaviors { get; } = new BehaviorsCollection();
        }
    }
}
