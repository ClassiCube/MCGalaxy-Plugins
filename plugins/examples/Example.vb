Imports System

Namespace MCGalaxy
    Public Class Example
        Inherits Plugin

        Public Overrides ReadOnly Property name() As String
            Get
                Return "Example"
            End Get
        End Property
        Public Overrides ReadOnly Property MCGalaxy_Version() As String
            Get
                Return "1.9.3.5"
            End Get
        End Property
        Public Overrides ReadOnly Property welcome() As String
            Get
                Return "Loaded Message!"
            End Get
        End Property
        Public Overrides ReadOnly Property creator() As String
            Get
                Return "Your name here"
            End Get
        End Property

        Public Overrides Sub Load(startup As Boolean)
            ' LOAD YOUR PLUGIN WITH EVENTS OR OTHER THINGS!
        End Sub

        Public Overrides Sub Unload(shutdown As Boolean)
            ' UNLOAD YOUR PLUGIN BY SAVING FILES OR DISPOSING OBJECTS!
        End Sub

        Public Overrides Sub Help(p As Player)
            ' HELP INFO!
        End Sub
    End Class
End Namespace
