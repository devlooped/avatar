Imports System.Diagnostics.CodeAnalysis
Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace Global.Stunts

    <CompilerGenerated>
    <ExcludeFromCodeCoverage>
    Partial Friend Class Stunt

        Shared Sub New()
            If StuntFactory.[Default] IsNot StuntFactory.NotImplemented Then Return

            Dim factoryAttribute = Assembly.GetExecutingAssembly().GetCustomAttributes(Of StuntFactoryAttribute)().FirstOrDefault()

            If factoryAttribute IsNot Nothing Then
                Dim factoryType = Type.[GetType](factoryAttribute.TypeName)
                If factoryType Is Nothing Then Throw New ArgumentException($"Stunt factory from provider {factoryAttribute.ProviderId} could not be loaded from {factoryAttribute.TypeName}.")
                StuntFactory.[Default] = CType(Activator.CreateInstance(factoryType), IStuntFactory)
            Else
                Throw New InvalidOperationException($"A valid stunt factory was not set as {NameOf(StuntFactory)}.{NameOf(StuntFactory.Default)} or registered for the current assembly with an [{NameOf(StuntFactoryAttribute)}] attribute.")
            End If
        End Sub

        Private Shared Function Create(Of T)(ByVal constructorArgs As Object(), ParamArray interfaces As Type()) As T
            Return DirectCast(StuntFactory.[Default].CreateStunt(GetType(Stunt).GetTypeInfo().Assembly, GetType(T), interfaces, constructorArgs), T)
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs)
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6, T7)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6), GetType(T7))
        End Function

        <StuntGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6, T7, T8)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6), GetType(T7), GetType(T8))
        End Function
    End Class
End Namespace