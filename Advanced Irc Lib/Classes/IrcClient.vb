﻿Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.IO

Public Class IrcClient
    Public WithEvents irc As New TcpClient
    Public Stream As System.Net.Sockets.NetworkStream

    'Events
    Public Event OnReciveData(ByVal Data As String)
    Public Event OnConnect()

    'Class vars
    Public isConnected As Boolean
    Public isInChannel As Boolean

    Public Structure IrcServer
        Public Server As String
        Public Port As Integer
    End Structure
    Public Structure ChannelMessageData
        Public Server As String
        Public Command As String
        Public Channel As String
        Public Message As String
    End Structure

    '<summary>  
    'Connection event, connects to server IP/Address and port. 2 Overloads.
    '</summary>
    Public Overloads Sub Connect(ByVal Server As String, ByVal Port As Integer)
        Try
            irc.Connect(Server, Port)
            Stream = irc.GetStream()
            isConnected = True
            RaiseEvent OnConnect()
        Catch e As Exception
            Return
        End Try
    End Sub
    Public Overloads Sub Connect(ByVal IrcServer As IrcServer)
        Try
            irc.Connect(IrcServer.Server, IrcServer.Port)
            Stream = irc.GetStream()
            isConnected = True
            RaiseEvent OnConnect()
        Catch e As Exception
            Return
        End Try
    End Sub

    '<summary>  
    'Return isConnected, boolean.
    '</summary>
    Public Function Connected() As Boolean
        If isConnected = True Then
            Return True
        Else
            Return False
        End If
    End Function

    '<summary>  
    'Sends data to currently connected irc server.
    '</summary>
    Public Sub SendData(ByVal Data As String)
        Dim Writer As New StreamWriter(Stream)
        Writer.WriteLine(Data, Stream)
        Writer.Flush()
    End Sub

    Public Function GetStream() As System.Net.Sockets.NetworkStream
        Return irc.GetStream()
    End Function

    Public Sub Listen()
        If isConnected = False Then
            Return
        End If

        Dim Reader As New StreamReader(Stream)
        Dim Data As String = Reader.ReadLine()
        While (isConnected = True And Data <> "")
            RaiseEvent OnReciveData(Data)
            Listen()
        End While

        If (InStr(Data, "PING")) Then
            SendData(Replace(Data, "PING", "PONG"))
        End If

        Reader.Dispose()
        Data = Nothing
        Listen()
    End Sub
    Public Sub Disconnect()
        irc.Close()
    End Sub
    Public Sub JoinChannel(ByVal Channel As String)
        SendData("JOIN #" & Channel)
        isInChannel = True
    End Sub
    Public Sub SetNick(ByVal Nick As String)
        SendData("NICK " & Nick)
    End Sub

    Public Function ReadChannelData(ByVal Data As String) As ChannelMessageData
        If isInChannel = False Then
            Return Nothing
        End If

        Try
            Dim msg As New ChannelMessageData
            Dim MessageArray() As String = Split(Data, " ", 4)

            msg.Server = MessageArray(0)
            msg.Command = MessageArray(1)
            msg.Channel = MessageArray(2)
            msg.Message = MessageArray(3)

            Return msg
        Catch e As Exception
            Return Nothing
        End Try

    End Function
    Public Function ReadMessage(ByVal Data As String) As String
        If isInChannel = False Then
            Return Nothing
        End If

        Dim msg As ChannelMessageData = ReadChannelData(Data)
        Return msg.Message
    End Function

End Class
