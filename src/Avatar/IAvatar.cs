using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avatars
{
    /// <summary>
    /// Interface implemented by all avatars.
    /// </summary>
    // These attributes prevent registering the "Implement through behavior pipeline" codefix.
    // See CustomMockCodeFixProvider and its base class CustomAvatarCodeFixProvider.
    [GeneratedCode("Avatar", ThisAssembly.Info.Version)]
    [CompilerGenerated]
    public interface IAvatar
    {
        /// <summary>
        /// Behaviors configured for the avatar.
        /// </summary>
        IList<IAvatarBehavior> Behaviors { get; }
    }
}