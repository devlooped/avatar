Imports System
Imports Avatars
Imports Xunit

Namespace Sample

    Public Class Tests

        <Fact>
        Public Sub CanConfigureDefaultValues()
            Dim calculator = Avatar.[Of](Of ICalculator, IDisposable)()

            Assert.IsNotType(Of StaticAvatarFactory)(AvatarFactory.[Default])

            Dim recorder = New RecordingBehavior()
            calculator.AddBehavior(recorder)
            calculator.AddBehavior(New DefaultValueBehavior())

            Assert.IsAssignableFrom(Of IDisposable)(calculator)

            Assert.Equal(0, calculator.Add(5, 10))
            Assert.Single(recorder.Invocations)

            Console.WriteLine(recorder.ToString())
        End Sub

    End Class

End Namespace