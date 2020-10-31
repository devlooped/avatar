Imports Avatars

Public Class Class1

    Public Sub Run()
        Dim o = Avatar.Of(Of IFormatProvider)
    End Sub

End Class

Namespace Global.Avatars

    Partial Class Avatar
        Private Shared Sub OnInitialized()
            Console.WriteLine("Hello")
        End Sub
    End Class

End Namespace