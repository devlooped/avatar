Public Class Class1

    Public Sub Run()
        Dim o = Stunt.Of(Of IFormatProvider)
    End Sub

End Class

Namespace Global.Stunts

    Partial Class Stunt
        Private Shared Sub OnInitialized()
            Console.WriteLine("Hello")
        End Sub
    End Class

End Namespace