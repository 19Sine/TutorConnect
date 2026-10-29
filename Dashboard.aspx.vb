Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Threading.Tasks
Imports System.Web.UI.WebControls
Imports TutorConnect.Data
Imports TutorConnect.MasterPages
Imports TutorConnect.Models
Imports TutorConnect.Security
Imports TutorConnect.Services

Namespace AdminArea

    ''' <summary>The administrator's landing screen: six platform figures.</summary>
    Public Class Dashboard
        Inherits BasePage

        Private ReadOnly _authentication As IAuthenticationService = New AuthenticationService()
        Private ReadOnly _statistics As New StatisticsRepository()

        Protected WithEvents litStudents As Literal
        Protected WithEvents litTutors As Literal
        Protected WithEvents litAwaiting As Literal
        Protected WithEvents litRequestsToday As Literal
        Protected WithEvents litBookingsWeek As Literal
        Protected WithEvents litAverageRating As Literal

        Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
            RegisterAsyncTask(New PageAsyncTask(AddressOf LoadStatisticsAsync))
        End Sub

        Private Async Function LoadStatisticsAsync() As Task
            Dim shell As AdminShell = TryCast(Master, AdminShell)
            If shell IsNot Nothing Then
                shell.PageSubtitle = "Platform overview"
            End If

            Dim account As ServiceResult(Of Person) =
                Await _authentication.GetAccountAsync(CurrentPersonId).ConfigureAwait(True)

            If shell IsNot Nothing Then
                shell.PageTitle = If(account.Success, $"Welcome, {account.Data.FirstName}!", "Welcome!")
            End If

            Dim stats As PlatformStatistics =
                Await _statistics.GetPlatformStatisticsAsync().ConfigureAwait(True)

            Dim culture As CultureInfo = CultureInfo.GetCultureInfo("en-ZA")

            litStudents.Text = stats.TotalStudents.ToString(culture)
            litTutors.Text = stats.TotalTutors.ToString(culture)
            litAwaiting.Text = stats.TutorsAwaitingApproval.ToString(culture)
            litRequestsToday.Text = stats.RequestsToday.ToString(culture)
            litBookingsWeek.Text = stats.BookingsThisWeek.ToString(culture)

            ' An em dash rather than "0.0": no reviews yet is not the same as a rating of zero.
            litAverageRating.Text = If(stats.AverageRating.HasValue,
                                       stats.AverageRating.Value.ToString("0.0", culture),
                                       "—")
        End Function

    End Class

End Namespace
