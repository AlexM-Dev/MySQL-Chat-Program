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
    Private EntriesCount As Integer = 0
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
                                                       Dim UTCTime As Date = Date.Now.ToUniversalTime
                                                       myCore_Writer.InsertInto(table,
                                                                                {(UTCTime - CDate("1/1/1970")).TotalMilliseconds,
                                                                                Username,
                                                                                MySqlHelper.EscapeString(txtMessage.Text),
                                                                                UTCTime.ToString("dd/MM/yyyy HH:mm:ss.fff")})
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
            Dim rows As List(Of Object()) = myCore_Reader.GetRows(table)

            Dim i As Integer = 0
            If rows.Count > EntriesCount Then
                For Each row As Object() In rows
                    If i >= EntriesCount Then
                        InvokeEx(Sub() txtHistory.AppendText(FormatString(row, chatformat) & vbCrLf))
                    End If
                    i += 1
                Next
                EntriesCount = rows.Count
            End If

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
            columns.Add(New Core.Column("ID", "BigInt"))
            columns.Add(New Core.Column("Name", "Text"))
            columns.Add(New Core.Column("Message", "LongText"))
            columns.Add(New Core.Column("TS", "Text"))
            myCore_Writer.CreateTable(table, columns, False)
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