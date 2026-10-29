Option Strict On
Option Explicit On

Imports System.Security.Principal
Imports System.Web
Imports System.Web.Security
Imports TutorConnect.Security
Imports TutorConnect.Utilities

''' <summary>
''' Application-level event handling.
''' </summary>
''' <remarks>
''' The important work here is <see cref="Application_PostAuthenticateRequest"/>, which
''' rebuilds a principal carrying the user's role from the Forms ticket. Without it the
''' &lt;location&gt; role rules in Web.config match nothing, because the default Forms
''' identity carries a name and no roles at all - so every signed-in user would pass every
''' role check (SEC-04).
''' </remarks>
Public Class Global_asax
    Inherits HttpApplication

    Private Sub Application_PostAuthenticateRequest(sender As Object, e As EventArgs)
        Dim context As HttpContext = HttpContext.Current
        If context Is Nothing Then Return

        Dim user As IPrincipal = context.User
        If user Is Nothing OrElse user.Identity Is Nothing OrElse Not user.Identity.IsAuthenticated Then Return

        Dim formsIdentity As FormsIdentity = TryCast(user.Identity, FormsIdentity)
        If formsIdentity Is Nothing Then Return

        Dim principal As IPrincipal = RoleHelper.BuildPrincipal(formsIdentity.Ticket)

        If principal Is Nothing Then
            ' The ticket is unreadable or carries a role this build does not know.
            ' Treat it as no credential at all rather than guessing.
            FormsAuthentication.SignOut()
            Return
        End If

        context.User = principal
        Threading.Thread.CurrentPrincipal = principal
    End Sub

    ''' <summary>
    ''' Turns a missing page into the friendly 404 for everyone, local or remote.
    ''' </summary>
    ''' <remarks>
    ''' customErrors is deliberately left at RemoteOnly so a developer on this machine
    ''' still gets the full yellow screen and stack trace for a genuine 500 - that detail
    ''' is worth keeping while the application is being built.
    '''
    ''' A 404 is different: there is nothing sensitive in "that page does not exist", and
    ''' a developer clicking a link to a screen scheduled for a later phase should see the
    ''' explanation rather than a server error. So 404 is handled here explicitly, which
    ''' RemoteOnly would otherwise suppress locally.
    '''
    ''' Detailed logging of real faults arrives with ErrorLogger in P9.
    ''' </remarks>
    Private Sub Application_Error(sender As Object, e As EventArgs)
        Dim context As HttpContext = HttpContext.Current
        If context Is Nothing Then Return

        Dim lastError As Exception = Server.GetLastError()
        Dim httpError As HttpException = TryCast(lastError, HttpException)

        If httpError Is Nothing OrElse httpError.GetHttpCode() <> 404 Then
            ' A genuine fault. Log it and hand the reference to the error page, so the
            ' number the user is asked to quote is the one written to the log.
            If lastError IsNot Nothing Then
                context.Items(ErrorLogger.ReferenceKey) = ErrorLogger.Log(lastError, context)
            End If
            Return
        End If

        Server.ClearError()

        context.Response.Clear()
        context.Response.StatusCode = 404
        ' Without this, IIS replaces the body with its own error page.
        context.Response.TrySkipIisCustomErrors = True

        Try
            ' Rewrite rather than redirect: the address bar keeps the URL that was not
            ' found, which is more useful to whoever is looking at it.
            context.Server.Transfer("~/ErrorPages/Error404.aspx", preserveForm:=False)
        Catch ex As Threading.ThreadAbortException
            ' Server.Transfer always raises this to end the current request. Expected.
        Catch ex As HttpException
            ' Transfer is not possible for some request types (a missing static file, for
            ' instance). Fall back to a redirect so the user still gets a real page.
            context.Response.Redirect("~/ErrorPages/Error404.aspx", endResponse:=False)
            context.ApplicationInstance.CompleteRequest()
        End Try
    End Sub

End Class
