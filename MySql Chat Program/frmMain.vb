Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports MySql.Wrapper
Imports MySql.Data.MySqlClient
Public Class frmMain
#Region "Essentials"
    ' Core setup.
    Private WithEvents myCore_Reader As Core
    Private WithEvents myCore_Writer As Core
    Dim asyncRead As New Threading.Thread(AddressOf Async_ReadMessages)

    ' Variables that are used at runtime (read & written to)
    Private Username As String = "Default"
    Private CurrentID As Integer = 0
    Private cancelOperation As Boolean = False
    Private isHistory As Boolean = True

    Private waitTime As Double
#End Region
#Region "Events"
    Private Sub txtHistory_GotFocus(sender As Object, e As System.EventArgs) Handles txtHistory.GotFocus
        HideCaret(txtHistory.Handle)
    End Sub
    Private Sub myCore_OperationCompleted(e As Core.WrapperEventArgs) Handles myCore_Reader.OperationCompleted,
                                                                                myCore_Writer.OperationCompleted
        If e.ErrorOccurred Then
            InvokeEx(Sub() txtHistory.AppendText($"[ERROR] {e.ErrorEx.Message}{vbCrLf}"))
        End If
    End Sub

    Private Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        If txtMessage.Text IsNot "" Then
            Dim sendThread As New Threading.Thread(Sub()
                                                       myCore_Writer.InsertInto(table, {myCore_Writer.GetLastID(table) + 1, Username, MySqlHelper.EscapeString(txtMessage.Text), Date.Now.ToUniversalTime.ToString})
                                                       InvokeEx(Sub() txtMessage.Clear())
                                                   End Sub)
            sendThread.Start()
        End If
    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim Username_Select As String = InputBox("Select a username (leave blank For 'Default')")
        If Username_Select IsNot "" Then Username = Username_Select

        Restart()

        waitTime = Math.Ceiling(myCore_Reader.Ping / 100) * 100

        InitDB()

        asyncRead.Start()
    End Sub

    Private Sub txtMessage_KeyDown(sender As Object, e As KeyEventArgs) Handles txtMessage.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress() = True
            e.Handled = True
            btnSend.PerformClick()
        End If
    End Sub

    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        cancelOperation = True
        myCore_Reader.Close()
        myCore_Writer.Close()
    End Sub
#End Region
#Region "Helpers"
    Private Sub Async_ReadMessages()
        While Not cancelOperation
            Dim newLastID As Integer = myCore_Reader.GetLastID(table)

            If CurrentID < newLastID Then
                For i As Integer = CurrentID + If(isHistory, 0, 1) To newLastID
                    Dim result As Object() = myCore_Reader.SelectFromID(table, i)
                    InvokeEx(Sub() txtHistory.AppendText(FormatString(result, chatformat) & vbCrLf))
                Next
            End If

            isHistory = False

            CurrentID = newLastID

            Threading.Thread.Sleep(waitTime)
        End While
    End Sub
    Private Function FormatString(result As Object(), ByVal input As String) As String
        Dim output As String = input
        With output
            output = .Replace("%name%", result(1)).Replace("%message%", result(2)).Replace("%time%", result(3))
        End With
        Return output
    End Function
    Private Sub InitDB()
        If Not myCore_Reader.ListTables.Contains(table) Then
            Dim columns As New List(Of Core.Column)
            columns.Add(New Core.Column("Name", Core.DataType.TEXT))
            columns.Add(New Core.Column("Message", Core.DataType.LONGTEXT))
            columns.Add(New Core.Column("TS", Core.DataType.TEXT))
            myCore_Writer.CreateTable(table, columns)
        End If
    End Sub
    Private Sub Restart()
        cancelOperation = True
        myCore_Reader = New Core(server, database, mysql_username, password)
        myCore_Writer = New Core(server, database, mysql_username, password)
        cancelOperation = False
    End Sub
#End Region
End Class