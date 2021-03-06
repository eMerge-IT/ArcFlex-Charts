﻿Imports System.Xml
Imports System.Collections
Imports System.Collections.Generic

Partial Class _Default
    Inherits System.Web.UI.Page

    Private vws As List(Of View)
    Public query As String = ""
    Private daysAgo As Integer

#Region "Page Lifecycle/Event Handlers"

    Protected Sub Page_PreInit(sender As Object, e As EventArgs) Handles Me.PreInit
        Using sr As New System.IO.StreamReader(Server.MapPath("~/workingquery.sql"))
            query = sr.ReadToEnd
        End Using

   
    End Sub

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim path As String = "graph.aspx" & "?d=" & TextBox1.Text
        Response.Redirect(path)
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ScriptManager.GetCurrent(Me).RegisterPostBackControl(TextBox1)

        If CInt(Request("d")) > 0 Then
            Session("daysago") = Request("d")
        ElseIf IsNothing(Session("daysago")) Then
            Session("daysago") = 14
        End If


        'this should be swapped for parameterized query, isn't that right @pajw?
        'http://xkcd.com/327/


        query &= "BETWEEN DATEADD(day, - " & Session("daysago") & ", GETDATE()) AND GETDATE()) "
        SqlDataSource1.SelectCommand = query
        Try
            vws = Views()
            If MultiView1.ActiveViewIndex = -1 Then
                MultiView1.ActiveViewIndex = 0
                If Not IsNothing(Grid(vws(MultiView1.ActiveViewIndex))) Then
                    If Not Page.IsPostBack() Then
                        Grid(vws(MultiView1.ActiveViewIndex)).DataBind()
                        SetCaption(Grid(vws(MultiView1.ActiveViewIndex)))
                    End If
                End If
                If Not IsNothing(Request("p")) Then
                    Try
                        Timer1.Interval = Request("p") * 1000
                    Catch
                    End Try
                End If
            End If
        Catch ex As Exception
            Response.Write(String.Format("{1}{0}{2}", "{br}", ex.Message, ex.StackTrace))
        End Try


    End Sub


    Protected Sub Timer1_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Timer1.Tick

        'If Not IsNothing(Grid(vws(MultiView1.ActiveViewIndex + 1))) Then
        '    If Grid(vws(MultiView1.ActiveViewIndex)).PageIndex < _
        '        Grid(vws(MultiView1.ActiveViewIndex)).PageCount - 1 Then
        '        Grid(vws(MultiView1.ActiveViewIndex)).PageIndex += 1
        '    Else
        '        Grid(vws(MultiView1.ActiveViewIndex)).PageIndex = 0
        '        If MultiView1.ActiveViewIndex >= vws.Count - 1 Then
        '            MultiView1.ActiveViewIndex = 0
        '        Else
        '            MultiView1.ActiveViewIndex += 1
        '        End If
        '        Grid(vws(MultiView1.ActiveViewIndex)).DataBind()
        '    End If
        '    SetCaption(Grid(vws(MultiView1.ActiveViewIndex)))
        'Else
        '    If MultiView1.ActiveViewIndex >= vws.Count - 1 Then
        '        MultiView1.ActiveViewIndex = 0
        '    Else
        '        MultiView1.ActiveViewIndex += 1
        '    End If
        'End If
        Try
            If Not IncrementGrid(Grid(vws(MultiView1.ActiveViewIndex))) Then IncrementView()
        Catch ex As Exception
            Response.Write(String.Format("{1}{0}{2}", "{br}", ex.Message, ex.StackTrace))
        End Try
    End Sub

#End Region
   

  

    Private Sub IncrementView()

        Dim retrycount As Integer = 0

        If MultiView1.ActiveViewIndex >= vws.Count - 1 Then
            MultiView1.ActiveViewIndex = 0
        Else
            MultiView1.ActiveViewIndex += 1
        End If
        While retrycount < 10
            Try
                If Not IsNothing(Grid(vws(MultiView1.ActiveViewIndex))) Then
                    Grid(vws(MultiView1.ActiveViewIndex)).DataBind()
                    SetCaption(Grid(vws(MultiView1.ActiveViewIndex)))
                End If
                retrycount += 10
            Catch ex As Exception
                ' A DIVZERO error may be caused by there being no data in the table
                ' whilst the data is refreshing

                ' Wait for two seconds.
                System.Threading.Thread.Sleep(2000)

                ' And then try again.            
                If retrycount < 10 Then
                    retrycount += 1
                Else
                    ' Return the error to the calling sub
                    Throw ex
                End If

            End Try
        End While
    End Sub

    Private Function IncrementGrid(ByVal grid As GridView) As Boolean
        If Not IsNothing(grid) Then
            If grid.PageIndex < grid.PageCount - 1 Then
                grid.PageIndex += 1
                SetCaption(grid)
                Return True
            Else
                grid.PageIndex = 0
                Return False
            End If
        End If
        Return False
    End Function

    Private Function Grid(ByVal vw As View) As GridView
        For Each c As Control In vw.Controls
            If c.ToString = "System.Web.UI.WebControls.GridView" Then
                Return c
                Exit Function
            End If
        Next
        Return Nothing
    End Function

    Private Function Views() As List(Of View)
        Try
            Dim ret As New List(Of View)
            For Each c As Control In MultiView1.Controls
                ret.Add(c)
            Next
            Return ret
        Catch ex As Exception
            Response.Write(String.Format("{1}{0}{2}", "{br}", ex.Message, ex.StackTrace))
            Return Nothing
        End Try
    End Function

    Private Sub SetCaption(ByVal gv As GridView)
        Try
            Dim cap As String = gv.Caption
            If InStr(cap, ">") Then
                cap = Trim(Split(Split(gv.Caption, "|")(0), ">")(1))
            Else
                cap = Trim(Split(gv.Caption, "|")(0))
            End If
            gv.Caption = String.Format("<div style={0}align: center; width: 99%; border-style: outset; font-family: Verdana; font-size: xx-large; color: #FFFFFF; background-color: #{4}{5}{6}; text-align: center;{0}> {1} | Page {2} of {3}</div><Br />", _
                                       Chr(34), _
                                       cap, _
                                       gv.PageIndex + 1, _
                                       gv.PageCount, _
                                       Right("00" & Hex(gv.HeaderStyle.BackColor.R), 2), _
                                       Right("00" & Hex(gv.HeaderStyle.BackColor.G), 2), _
                                       Right("00" & Hex(gv.HeaderStyle.BackColor.B), 2) _
            )
        Catch ex As Exception
            Response.Write(String.Format("{1}{0}{2}", "{br}", ex.Message, ex.StackTrace))
        End Try
    End Sub

    Dim totalQuotes As Decimal = 0
    Dim totalIvs As Decimal = 0

    'Protected Sub SumColumns(ByVal sender As Object, ByVal e As GridViewRowEventArgs) Handles GridView1.RowDataBound
    '    Select Case e.Row.RowType
    '        Case DataControlRowType.DataRow
    '            If IsNumeric(e.Row.Cells(1).Text.Replace(",", "").Replace("&#163;", "")) Then totalQuotes += CDbl(e.Row.Cells(1).Text.Replace(",", "").Replace("&#163;", ""))
    '            If IsNumeric(e.Row.Cells(3).Text.Replace(",", "").Replace("&#163;", "")) Then totalIvs += CDbl(e.Row.Cells(3).Text.Replace(",", "").Replace("&#163;", ""))
    '        Case DataControlRowType.Footer
    '            With e.Row.Cells(1)
    '                .Text = String.Format("{0}", totalQuotes.ToString("###,###,###"))
    '                .HorizontalAlign = HorizontalAlign.Center
    '            End With
    '            With e.Row.Cells(3)
    '                .Text = String.Format("{0}", totalIvs.ToString("###,###,###"))
    '                .HorizontalAlign = HorizontalAlign.Center
    '            End With
    '    End Select
    'End Sub


End Class
