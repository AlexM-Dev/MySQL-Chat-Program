Imports System.ComponentModel

Module [Global]
    Friend Const server As String = "localhost"
    Friend Const database As String = "chat"
    Friend Const mysql_username As String = "root"
    Friend Const password As String = ""
    Friend Const table As String = "freechat4"

    Friend Const chatformat As String = "[%time%] %name%: %message%"

    <System.Runtime.CompilerServices.Extension>
    Friend Sub InvokeEx(Of T As ISynchronizeInvoke)(this As T, action As Action(Of T))
        If this.InvokeRequired Then
            this.Invoke(action, New Object() {this})
        Else
            action(this)
        End If
    End Sub
End Module
