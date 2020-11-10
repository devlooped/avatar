using System;

namespace Avatars
{
    /// <summary>
    /// Usability functions for working with avatars.
    /// </summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    public static class AvatarExtensions
    {
        /// <summary>
        /// Adds a behavior to an avatar.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="behavior">(invocation, next) => invocation.CreateValueReturn() | invocation.CreateExceptionReturn() | next().Invoke(invocation, next)</param>
        /// <param name="appliesTo">invocation => true|false</param>
        /// <param name="name">Optional friendly name for the behavior.</param>
        public static IAvatar AddBehavior(this IAvatar avatar, ExecuteDelegate behavior, AppliesToDelegate? appliesTo = null, string? name = null)
        {
            avatar.Behaviors.Add(new AnonymousBehavior(behavior, appliesTo, name));
            return avatar;
        }

        /// <summary>
        /// Adds a behavior to an avatar.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="behavior">A custom behavior to apply to the avatar.</param>
        public static IAvatar AddBehavior(this IAvatar avatar, IAvatarBehavior behavior)
        {
            avatar.Behaviors.Add(behavior);
            return avatar;
        }

        /// <summary>
        /// Adds a behavior to an avatar.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="behavior">(invocation, next) => invocation.CreateValueReturn() | invocation.CreateExceptionReturn() | next().Invoke(invocation, next)</param>
        /// <param name="appliesTo">invocation => true|false</param>
        /// <param name="name">Optional friendly name for the behavior.</param>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TAvatar AddBehavior<TAvatar>(this TAvatar avatar, ExecuteDelegate behavior, AppliesToDelegate? appliesTo = null, string? name = null)
        {
            // We can't just add a constraint to the method signature, because 
            // proxies are typically generated and don't expose the IProxy interface directly.
            if (avatar is IAvatar target)
                target.Behaviors.Add(new AnonymousBehavior(behavior, appliesTo, name));
            else
                throw new ArgumentException(nameof(avatar));

            return avatar;
        }

        /// <summary>
        /// Adds a behavior to a avatar.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="behavior">A custom behavior to apply to the avatar.</param>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TAvatar AddBehavior<TAvatar>(this TAvatar avatar, IAvatarBehavior behavior)
        {
            if (avatar is IAvatar target)
                target.Behaviors.Add(behavior);
            else
                throw new ArgumentException(nameof(avatar));

            return avatar;
        }

        /// <summary>
        /// Inserts a behavior into the avatar behavior pipeline at the specified 
        /// index.
        /// </summary>
        /// <param name="avatar">The avatar to insert the behavior to.</param>
        /// <param name="index">The index to insert the behavior at.</param>
        /// <param name="behavior">(invocation, next) => invocation.CreateValueReturn() | invocation.CreateExceptionReturn() | next().Invoke(invocation, next)</param>
        /// <param name="appliesTo">invocation => true|false</param>
        /// <param name="name">Optional friendly name for the behavior.</param>
        public static IAvatar InsertBehavior(this IAvatar avatar, int index, ExecuteDelegate behavior, AppliesToDelegate? appliesTo = null, string? name = null)
        {
            avatar.Behaviors.Insert(index, new AnonymousBehavior(behavior, appliesTo, name));
            return avatar;
        }

        /// <summary>
        /// Inserts a behavior into the avatar behavior pipeline at the specified 
        /// index.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="index">The index to insert the behavior at.</param>
        /// <param name="behavior">A custom behavior to apply to the avatar.</param>
        public static IAvatar InsertBehavior(this IAvatar avatar, int index, IAvatarBehavior behavior)
        {
            avatar.Behaviors.Insert(index, behavior);
            return avatar;
        }

        /// <summary>
        /// Inserts a behavior into the avatar behavior pipeline at the specified
        /// index.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="index">The index to insert the behavior at.</param>
        /// <param name="behavior">(invocation, next) => invocation.CreateValueReturn() | invocation.CreateExceptionReturn() | next().Invoke(invocation, next)</param>
        /// <param name="appliesTo">invocation => true|false</param>
        /// <param name="name">Optional friendly name for the behavior.</param>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TAvatar InsertBehavior<TAvatar>(this TAvatar avatar, int index, ExecuteDelegate behavior, AppliesToDelegate? appliesTo = null, string? name = null)
        {
            if (avatar is IAvatar target)
                target.Behaviors.Insert(index, new AnonymousBehavior(behavior, appliesTo, name));
            else
                throw new ArgumentException(nameof(avatar));

            return avatar;
        }

        /// <summary>
        /// Inserts a behavior into the avatar behavior pipeline at the specified
        /// index.
        /// </summary>
        /// <param name="avatar">The avatar to add the behavior to.</param>
        /// <param name="index">The index to insert the behavior at.</param>
        /// <param name="behavior">A custom behavior to apply to the avatar.</param>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TAvatar InsertBehavior<TAvatar>(this TAvatar avatar, int index, IAvatarBehavior behavior)
        {
            if (avatar is IAvatar target)
                target.Behaviors.Insert(index, behavior);
            else
                throw new ArgumentException(nameof(avatar));

            return avatar;
        }
    }
}
