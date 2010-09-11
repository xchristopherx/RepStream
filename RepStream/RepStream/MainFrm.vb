Option Explicit On
'Option Strict On
Imports System
Imports System.IO
Imports System.IO.Ports

Public Class MainFrm
    Dim WithEvents comport As New SerialPort
    Dim comnum As String
    Dim baudrate As Integer
    Dim okToSend As Boolean = False
    Dim pause As Boolean = False
    Dim ArrayHolder As Integer = 0
    Dim lineArray() As String
    Dim stopit As Boolean = False

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        For Each sp As String In My.Computer.Ports.SerialPortNames
            ComboBox1.Items.Add(sp)
        Next
        'ComboBox1.SelectedIndex = 1
        If My.Settings.Comport <> vbNullString Then
            ComboBox1.SelectedText = My.Settings.Comport
        End If
        If My.Settings.Baud <> vbNullString Then
            txtBaud.Text = My.Settings.Baud
        End If

    End Sub

    Private Sub Connect()
        comnum = ComboBox1.Text

        btnConnect.Enabled = False
        If txtBaud.Text <> vbNullString And comnum <> vbNullString Then
            baudrate = CType(txtBaud.Text, Integer)
            readcom()
        End If

    End Sub

    Private Sub readcom()
        Try
            With comport
                .PortName = comnum
                .BaudRate = baudrate
                .Parity = Parity.None
                .DataBits = 8
                .StopBits = StopBits.One
            End With
            comport.Open()
            btnSingle.Enabled = True
            btnGCode.Enabled = True
        Catch ex As Exception
            MsgBox(ex.ToString)
            btnSingle.Enabled = False
            btnGCode.Enabled = False
        End Try
    End Sub

    Private Sub comport_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles comport.DataReceived
        Dim Data As String
        Data = comport.ReadLine
        Try
            Do Until Data Is Nothing
                'Application.DoEvents()
                ReceiveSerialData(Data)
                Data = comport.ReadLine
            Loop
        Catch ex As Exception
        End Try
    End Sub

    Sub SendSerialData(ByVal Data As String)
        comport.WriteLine(Data)
    End Sub

    Sub ReceiveSerialData(ByVal Data As String)
        CheckForIllegalCrossThreadCalls = False
        txtLog.Text &= Data & vbCrLf
        txtLog.SelectionStart = txtLog.Text.Length
        txtLog.ScrollToCaret()
        If InStr(Data, "ok") > 0 Or InStr(Data, "T:") > 0 Then
            okToSend = True
        End If
    End Sub

    Private Sub btnSingle_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSingle.Click
        If txtSingle.Text <> vbNullString Then
            SendSerialData(txtSingle.Text)
        Else
            txtLog.Text = "ERROR: Type something first before sending command"
        End If
    End Sub

    Private Sub btnConnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnConnect.Click
        Connect()
    End Sub

    Private Sub txtBaud_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtBaud.KeyPress
        Dim allowedChars As String = "0123456789" & vbBack
        If allowedChars.IndexOf(e.KeyChar) = -1 Then
            e.Handled = True
        End If
    End Sub

    Private Sub btnGcode_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGCode.Click
        btnGCode.Enabled = False
        btnPause.Enabled = True
        btnReload.Enabled = True
        lineArray = txtGCode.Text.Split(vbCrLf)
        startSending()
    End Sub

    Private Sub startSending()
        Dim i As Integer
        For i = ArrayHolder To UBound(lineArray)
            'send line, wait for ok
            'check null
            If stopit = True Then
                ArrayHolder = 0
                Exit Sub
            End If
            If pause = True Then
                ArrayHolder = i
                Exit Sub
            End If
            lineArray(i) = Trim(lineArray(i))
            If lineArray(i) <> vbNullString Then
                If InStr(lineArray(i), ";") > 0 Then
                    lineArray(i) = Split(lineArray(i), ";")(0)
                End If
                SendSerialData(lineArray(i))
                While okToSend = False
                    Application.DoEvents()
                    If stopit = True Then
                        ArrayHolder = 0
                        Exit Sub
                    End If
                    If pause = True Then
                        ArrayHolder = i
                        Exit Sub
                    End If
                End While
                okToSend = False
            End If
        Next
        txtLog.Text &= "Finished."
        btnGCode.Enabled = True
    End Sub

    Private Sub btnPause_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPause.Click
        pause = True
        btnResume.Enabled = True
        btnPause.Enabled = False

    End Sub

    Private Sub btnResume_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnResume.Click
        pause = False
        btnPause.Enabled = True
        btnResume.Enabled = False
    End Sub

    Private Sub btnReload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReload.Click
        'hacky, probably has a race condition
        stopit = True
        ArrayHolder = 0
        startSending()
    End Sub

    Private Sub LoadGcodeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LoadGcodeToolStripMenuItem.Click
        txtGCode.Text = ""

        ' OpenFileDialog1.InitialDirectory = "c:\"
        OpenFileDialog1.Filter = "gcode files (*.gcode)|*.gcode|txt files (*.txt)|*.txt|All files (*.*)|*.*"
        OpenFileDialog1.FilterIndex = 1
        OpenFileDialog1.RestoreDirectory = True
        OpenFileDialog1.FileName = ""

        Dim gcodetext As String
        If OpenFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            Try
                Using reader As New StreamReader(OpenFileDialog1.FileName)
                    gcodetext = reader.ReadToEnd
                    txtGCode.Text = gcodetext
                End Using
            Catch Ex As Exception
                MessageBox.Show("Cannot read file from disk. Original error: " & Ex.Message)
            End Try
        End If
    End Sub

    Private Sub SaveGcodeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveGcodeToolStripMenuItem.Click
        SaveFileDialog1.Filter = "gcode files (*.gcode)|*.gcode|txt files (*.txt)|*.txt|All files (*.*)|*.*"
        SaveFileDialog1.FilterIndex = 1
        SaveFileDialog1.RestoreDirectory = True
        Dim temp As String
        temp = txtGCode.Text.ToString
        If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
            Using outfile As New StreamWriter(SaveFileDialog1.FileName)
                outfile.Write(temp)
            End Using
        End If

    End Sub

    Private Sub SaveSettingsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveSettingsToolStripMenuItem.Click
        My.Settings.Baud = txtBaud.Text
        My.Settings.Comport = ComboBox1.Text
        My.Settings.Save()
        MsgBox("Settings saved.")
    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        If comport.IsOpen Then
            comport.Close()
        End If
        End
    End Sub

    Private Sub frmMain_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If comport.IsOpen Then
            comport.Close()
        End If
        End
    End Sub
End Class
