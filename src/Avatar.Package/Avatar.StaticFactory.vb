Namespace Global.Avatars
    Partial Friend Class Avatar
        Shared Sub New()
            AvatarFactory.[Default] = New StaticAvatarFactory
            OnInitialized()
        End Sub

        ''' <summary>
        ''' Invoked after the default <see cref="AvatarFactory.Default"/> 
        ''' is initialized.
        ''' </summary>
        Partial Private Shared Sub OnInitialized()

        End Sub
    End Class
End Namespace