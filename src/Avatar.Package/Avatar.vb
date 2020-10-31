Imports System.Diagnostics.CodeAnalysis
Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace Global.Avatars

    <CompilerGenerated>
    <ExcludeFromCodeCoverage>
    Partial Friend Class Avatar
        Private Shared Function Create(Of T)(ByVal constructorArgs As Object(), ParamArray interfaces As Type()) As T
            Return DirectCast(AvatarFactory.[Default].CreateAvatar(GetType(Avatar).Assembly, GetType(T), interfaces, constructorArgs), T)
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs)
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6, T7)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6), GetType(T7))
        End Function

        <AvatarGenerator>
        Public Shared Function [Of](Of T, T1, T2, T3, T4, T5, T6, T7, T8)(ParamArray constructorArgs As Object()) As T
            Return Create(Of T)(constructorArgs, GetType(T1), GetType(T2), GetType(T3), GetType(T4), GetType(T5), GetType(T6), GetType(T7), GetType(T8))
        End Function
    End Class
End Namespace