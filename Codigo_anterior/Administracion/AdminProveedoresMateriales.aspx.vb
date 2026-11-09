Imports System.Collections.Generic
Imports co.com.Arquitectos.DataObject
Imports co.com.Arquitectos.BussinesObject
Imports System.Data.SqlClient
Partial Class Administracion_AdminProveedoresMateriales
    Inherits Culture

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim cadenaconexion As String
        cadenaconexion = ConfigurationManager.ConnectionStrings("Arquitectos").ToString()

        If Page.IsPostBack = False Then

            listardatos()

        End If


    End Sub


    Protected Sub btnQuitar_Click(sender As Object, e As EventArgs) Handles btnQuitar.Click


        Dim ingresado As Boolean = False

        For Each row As GridViewRow In gvAgregados.Rows
            Dim texto As CheckBox = (CType(row.FindControl("ckbSelect"), CheckBox))
            Dim proveedor As String = ddlProveedores.SelectedValue.ToString()
            If (texto.Checked) Then

                Dim codigoproducto As String = row.Cells(1).Text.ToString()

                Dim informes As Informes = New Informes()
                Dim sql As String = ""

                sql = " delete from Lista_Materiales_Proveedores  where codigoproducto ='" + codigoproducto + "' and codigoproveedor='" + proveedor + "'"

                If (informes.EjecutaConsultas(sql)) Then
                    ingresado = True
                End If

                listardatos()
                If (ingresado) Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Finalizado!', '¡ Quitados correctamente !', 'success')", True)
                Else
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Ocurrio un error con un material que esta agregando !', 'error')", True)

                End If
            End If
        Next
    End Sub
    Protected Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
        Dim ingresado As Boolean = False

        For Each row As GridViewRow In gvSinAgregar.Rows
            Dim cantidad As TextBox = (CType(row.FindControl("txtCantidad"), TextBox))
            Dim texto As CheckBox = (CType(row.FindControl("ckbSelect"), CheckBox))
            Dim proveedor As String = ddlProveedores.SelectedValue.ToString()
            If (texto.Checked) Then

                Dim codigoproducto As String = row.Cells(1).Text.ToString()

                If (cantidad.Text = "") Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Debe seleccionar valores superiores a cero !', 'error')", True)
                    Return
                Else
                    Dim cantidadnueva As String = cantidad.Text.Replace(".", "").Replace(",", "")
                    Dim informes As Informes = New Informes()
                    Dim sql As String = ""

                    sql = " insert into Lista_Materiales_Proveedores values ('" + codigoproducto + "','" + cantidadnueva + "','" + proveedor + "')"

                    If (informes.EjecutaConsultas(sql)) Then
                        ingresado = True
                    End If

                End If
                listardatos()
                If (ingresado) Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Finalizado!', '¡ Materiales agregados correctamente !', 'success')", True)
                Else
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Ocurrio un error con un material que esta agregando !', 'error')", True)

                End If
            Else
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "", "swal('¡Error!', '¡ Debe seleccionar al menos un material para agregar !', 'error')", True)


            End If


        Next
    End Sub


    Protected Sub listardatos()
        dsAgregados.SelectParameters("proveedor").DefaultValue = ddlProveedores.SelectedValue.ToString()
        dsAgregados.DataBind()
        gvAgregados.DataBind()

        dsSinAgregar.SelectParameters("proveedor").DefaultValue = ddlProveedores.SelectedValue.ToString()
        dsSinAgregar.DataBind()
        gvSinAgregar.DataBind()
    End Sub

    Protected Sub ddlProveedores_TextChanged(sender As Object, e As EventArgs)

        listardatos()
    End Sub

End Class
