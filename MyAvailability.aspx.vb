Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Threading.Tasks
Imports System.Web
Imports System.Web.UI.WebControls
Imports TutorConnect.Controls
Imports TutorConnect.MasterPages
Imports TutorConnect.Models
Imports TutorConnect.Security
Imports TutorConnect.Services

Namespace TutorArea

    ''' <summary>
    ''' Publish and remove time slots (BR-06, BR-07, BR-20, BR-22).
    ''' </summary>
    Public Class MyAvailability
        Inherits TutorPageBase

        Private ReadOnly _availability As IAvailabilityService = New AvailabilityService()
        Private ReadOnly _tutors As ITutorService = New TutorService()

        Protected WithEvents alert As AlertMessage
        Protected WithEvents ddlSubject As DropDownList
        Protected WithEvents txtDate As TextBox
        Protected WithEvents txtStart As TextBox
        Protected WithEvents txtEnd As TextBox
        Protected WithEvents btnAdd As Button
        Protected WithEvents rptSlots As Repeater
        Protected WithEvents pnlEmpty As Panel
        Protected WithEvents btnRefresh As Button

        Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
            Dim shell As TutorShell = TryCast(Master, TutorShell)
            If shell IsNot Nothing Then
                shell.PageTitle = "My Availability"
                shell.PageSubtitle = "Set your available times"
            End If

            RegisterAsyncTask(New PageAsyncTask(AddressOf LoadAsync))
        End Sub

        Private Async Function LoadAsync() As Task
            Dim tutorId As Integer = Await ResolveTutorIdAsync()
            If tutorId = 0 Then
                alert.Show(AlertKind.Error, "No tutor profile is linked to this account.")
                btnAdd.Enabled = False
                Return
            End If

            If Not IsPostBack Then
                Dim subjects As ServiceResult(Of IList(Of TutorSubject)) =
                    Await _tutors.GetSubjectsAsync(tutorId).ConfigureAwait(True)

                ddlSubject.Items.Clear()
                For Each ts In subjects.Data
                    ddlSubject.Items.Add(New ListItem(
                        $"{ts.SubjectName} - {ts.StudyLevelName}",
                        ts.TutorSubjectId.ToString(CultureInfo.InvariantCulture)))
                Next

                If subjects.Data.Count = 0 Then
                    btnAdd.Enabled = False
                    alert.Show(AlertKind.Info,
                               "Add a subject on My Subjects before publishing available times.")
                End If
            End If

            Await BindAsync()
        End Function

        Private Async Function BindAsync() As Task
            Dim tutorId As Integer = Await ResolveTutorIdAsync()

            Dim result As ServiceResult(Of IList(Of TutorAvailability)) =
                Await _availability.GetForTutorAsync(tutorId).ConfigureAwait(True)

            rptSlots.DataSource = result.Data
            rptSlots.DataBind()

            rptSlots.Visible = result.Data.Count > 0
            pnlEmpty.Visible = result.Data.Count = 0
        End Function

        Protected Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
            RegisterAsyncTask(New PageAsyncTask(AddressOf AddAsync))
        End Sub

        Private Async Function AddAsync() As Task
            Dim tutorId As Integer = Await ResolveTutorIdAsync()

            Dim slotDate As Date
            Dim startTime As TimeSpan
            Dim endTime As TimeSpan

            ' TryParseExact on user input, never Parse: a malformed value must produce a
            ' message, not an unhandled exception.
            If Not Date.TryParseExact(txtDate.Text, {"yyyy-MM-dd", "dd/MM/yyyy"},
                                      CultureInfo.GetCultureInfo("en-ZA"),
                                      DateTimeStyles.None, slotDate) Then
                alert.Show(AlertKind.Error, "Enter a valid date.")
                Return
            End If

            If Not TimeSpan.TryParseExact(txtStart.Text, {"hh\:mm", "h\:mm"},
                                          CultureInfo.InvariantCulture, startTime) Then
                alert.Show(AlertKind.Error, "Enter a valid start time, for example 10:00.")
                Return
            End If

            If Not TimeSpan.TryParseExact(txtEnd.Text, {"hh\:mm", "h\:mm"},
                                          CultureInfo.InvariantCulture, endTime) Then
                alert.Show(AlertKind.Error, "Enter a valid end time, for example 12:00.")
                Return
            End If

            Dim tutorSubjectId As Integer
            Integer.TryParse(ddlSubject.SelectedValue, tutorSubjectId)

            Dim result As ServiceResult(Of Integer) = Await _availability.AddAsync(
                tutorId, tutorSubjectId, slotDate, startTime, endTime).ConfigureAwait(True)

            alert.Show(If(result.Success, AlertKind.Success, AlertKind.Error), result.Message)

            If result.Success Then
                txtDate.Text = String.Empty
                txtStart.Text = String.Empty
                txtEnd.Text = String.Empty
            End If

            Await BindAsync()
        End Function

        Protected Sub Delete_Command(sender As Object, e As CommandEventArgs)
            Dim availabilityId As Integer
            Integer.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), availabilityId)

            RegisterAsyncTask(New PageAsyncTask(
                Async Function() As Task
                    Dim tutorId As Integer = Await ResolveTutorIdAsync()

                    Dim result As ServiceResult =
                        Await _availability.DeleteAsync(tutorId, availabilityId).ConfigureAwait(True)

                    alert.Show(If(result.Success, AlertKind.Success, AlertKind.Error), result.Message)
                    Await BindAsync()
                End Function))
        End Sub

        Protected Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
            alert.Hide()
        End Sub

#Region "Row helpers"

        Protected Shared Function GetDate(value As Object) As String
            Dim d As Date = Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            Return HttpUtility.HtmlEncode(d.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("en-ZA")))
        End Function

        Protected Shared Function GetTime(value As Object) As String
            Dim t As TimeSpan = CType(value, TimeSpan)
            Return HttpUtility.HtmlEncode(t.ToString("hh\:mm", CultureInfo.InvariantCulture))
        End Function

        ''' <summary>Words, not a tick: colour or shape alone is not an accessible signal.</summary>
        Protected Shared Function GetBooked(value As Object) As String
            Return If(Convert.ToBoolean(value, CultureInfo.InvariantCulture), "Booked", "Open")
        End Function

#End Region

    End Class

End Namespace
