Imports System.Collections.Generic
Imports co.com.Arquitectos.DataObject
Imports co.com.Arquitectos.BussinesObject
Imports System.Data.SqlClient
Partial Class Administracion_AdminArquitectos
    Inherits Culture

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim cadenaconexion As String
        cadenaconexion = ConfigurationManager.ConnectionStrings("Arquitectos").ToString()

        If Page.IsPostBack = False Then

            cargarArquitectos()

        End If
    End Sub
    Sub cargarArquitectos()
        dsArquitectos.DataBind()
        gvArquitectos.DataBind()

    End Sub

    Protected Sub gvArquitectos_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles gvArquitectos.SelectedIndexChanged

        Dim codigo As String = ""

        codigo = gvArquitectos.SelectedValue.ToString()

        If String.IsNullOrEmpty(codigo) = False Then
            Dim nombre As String

            nombre = gvArquitectos.SelectedRow.Cells(2).Text.ToString()

            txtNombre.Text = nombre.Trim()

            btnEliminar.Visible = True
            btnModificar.Visible = True
            btnCrear.Visible = False
        End If
    End Sub

    Protected Sub btnCrear_Click(sender As Object, e As EventArgs) Handles btnCrear.Click

        Dim cadenaconexion As String = ConfigurationManager.ConnectionStrings("Arquitectos").ToString()
        Dim arquitectosBO As AdminAquitectosBO = New AdminAquitectosBO(cadenaconexion)
        Dim resultado As Boolean = False
        Dim existe As Boolean = False

        existe = arquitectosBO.verificaArquitecto(txtNombre.Text)

        If existe = True Then
            txtNombre.Text = ""
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡" + Resources.multi.language.Advertencia.ToString() + "!', '¡Ya existe un arquitecto con el mismo nombre !', 'warning')", True)
        Else

            resultado = arquitectosBO.ingresaArquitecto(txtNombre.Text)

            If resultado = True Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Finalizao!', '¡ Arquitecto creado correctamente !', 'success')", True)
            Else
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Ocurrio un error en la creación !', 'error')", True)
            End If

            txtNombre.Text = ""

            cargarArquitectos()
        End If

    End Sub

    Protected Sub btnModificar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnModificar.Click

        Dim cadenaconexion As String = ConfigurationManager.ConnectionStrings("Arquitectos").ToString()
        Dim arquitectosBO As AdminAquitectosBO = New AdminAquitectosBO(cadenaconexion)
        Dim resultado As Boolean = False
        Dim existe As Boolean = False
        Dim codigo As String = ""

        codigo = gvArquitectos.SelectedRow.Cells(1).Text.ToString()

        resultado = arquitectosBO.modificarArquitecto(txtNombre.Text, codigo)

        If resultado = True Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Finalizao!', '¡ Arquitecto acutalizado correctamente !', 'success')", True)
        Else
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡Ocurrio un error en la actualización !', 'error')", True)
        End If

        txtNombre.Text = ""

        btnEliminar.Visible = False
        btnModificar.Visible = False
        btnCrear.Visible = True
        cargarArquitectos()
    End Sub

    Protected Sub btnEliminar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEliminar.Click

        Dim cadenaconexion As String = ConfigurationManager.ConnectionStrings("Arquitectos").ToString()
        Dim arquitectosBO As AdminAquitectosBO = New AdminAquitectosBO(cadenaconexion)
        Dim codigo As String
        Dim resultado As Boolean = False

        codigo = gvArquitectos.SelectedRow.Cells(1).Text.ToString()

        If String.IsNullOrEmpty(codigo) = False Then
            resultado = arquitectosBO.elimimarUsuario(codigo)
            If resultado = True Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡" + Resources.multi.language.finalizado.ToString() + "!', '¡ Arquitecto Eliminado !', 'success')", True)
            Else
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Ocurrio un error al realizar la eliminación !', 'error')", True)
            End If
        Else
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡" + Resources.multi.language.Advertencia.ToString() + "!', '¡ Debe seleccionar al menos un arquitecto para eliminar  !', 'warning')", True)
        End If

        txtNombre.Text = ""

        btnEliminar.Visible = False
        btnModificar.Visible = False
        btnCrear.Visible = True
        cargarArquitectos()

    End Sub

    Protected Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
        txtNombre.Text = ""

        btnEliminar.Visible = False
        btnModificar.Visible = False
        btnCrear.Visible = True
    End Sub

    Protected Sub gvArquitectos_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvArquitectos.PageIndexChanging
        gvArquitectos.PageIndex = e.NewPageIndex
        cargarArquitectos()
    End Sub

End Class
