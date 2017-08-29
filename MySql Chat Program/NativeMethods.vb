Imports System.Runtime.InteropServices

Module NativeMethods
    <DllImport("user32")> Friend Function HideCaret(ByVal hWnd As IntPtr) As Integer
    End Function
End Module
