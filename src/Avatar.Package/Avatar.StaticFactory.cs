namespace Avatars
{
    partial class Avatar
    {
        static Avatar()
        {
            AvatarFactory.Default = new StaticAvatarFactory();
            OnInitialized();
        }

        /// <summary>
        /// Invoked after the default <see cref="AvatarFactory.Default"/> 
        /// is initialized.
        /// </summary>
        static partial void OnInitialized();
    }
}
