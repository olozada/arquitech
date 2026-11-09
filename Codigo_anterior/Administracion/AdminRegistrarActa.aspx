<%@ page title="" language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Administracion_AdminRegistrarActa, App_Web_d4eebhyk" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa fa-cogs " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="Toma de actas"></asp:Label>
                </small>
            </h1>

            <%--PARA SELECCIONAR EL ARQUITECTO,REVISOR,CONTRATISTA--%>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="Perfíl Arquitecto"></asp:Label>
                </div>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <table style="margin: auto;">
                            <tr runat="server" id="trFechaInicial">
                                <td>
                                    <b>
                                        <asp:Label ID="lblFechaInicial" runat="server" Text="Fecha inicial"></asp:Label>
                                    </b>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtFechaInicial" runat="server" Width="160px" CssClass='form-control'></asp:TextBox>
                                </td>
                                <td>
                                    <i class="fa fa-calendar" aria-hidden="true" id="Image1" runat="server"></i>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator10" runat="server" ErrorMessage="*"
                                        ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaInicial"></asp:RequiredFieldValidator>
                                    <cc1:CalendarExtender ID="CalendarExtender1" runat="server" TargetControlID="txtFechaInicial"
                                        PopupButtonID="Image1">
                                    </cc1:CalendarExtender>
                                </td>
                            </tr>
                            <tr runat="server" id="trFechaFinal">
                                <td>
                                    <b>
                                        <asp:Label ID="lblFechaFinal" runat="server" Text="Fecha final"></asp:Label>
                                    </b>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtFechaFinal" runat="server" Width="160px" CssClass='form-control'></asp:TextBox>
                                </td>
                                <td>
                                    <i class="fa fa-calendar" aria-hidden="true" id="imgb1" runat="server"></i>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*"
                                        ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaFinal"></asp:RequiredFieldValidator>

                                    <cc1:CalendarExtender ID="CalendarExtender2" runat="server" TargetControlID="txtFechaFinal"
                                        PopupButtonID="imgb1">
                                    </cc1:CalendarExtender>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Arquitecto:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlArquitectos" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsArquitectos"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsArquitectos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlArquitectosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Revisor:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlRevisores" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsRevisores"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsRevisores" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlRevisoresSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Contratista:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlContratista" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsContratistas"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsContratistas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlContratistasSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                          <%--  <tr>
                                <td>
                                    <asp:Label runat="server" Text="Construcción:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlConstruccion" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsConstruccion"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                   
                                </td>
                            </tr>--%>

                        </table>
                        <br />
                        <div style="text-align: center">
                            <asp:Button ID="btnCrear" runat="server" Text="Crear" CssClass="btn btn-success btn-sm" Width="100px" />
                            <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CausesValidation="False"
                                CssClass="btn btn-primary btn-sm" Width="100px" />
                        </div>
                    </div>
                </div>
            </div>
            <%--TERMINO DE REVISAR EL ARQUITECTO--%>

            <asp:Panel ID="panelDetalle" runat="server" BackColor="White" CssClass="modalPopUp" Height="735px"
                ScrollBars="Auto" Width="871px">
                <p style="text-align: right">
                    <asp:Button ID="btnCancelarDetalle" runat="server" Text="X" CausesValidation="False"
                        CssClass="btn btn-danger btn-sm" />
                </p>
                <%--PARA AGREGAR ITEMS--%>
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="Datos de items"></asp:Label>
                    </div>
                    <div class="panel panel-body">
                        <table>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Arquitecto:"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="" ID="lbArquitectoModal"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="Revisor:"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="" ID="lbRevisorModal"></asp:Label>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Contratista:"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="" ID="lbContratistaModal"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="Construcción:"></asp:Label>
                                </td>
                                <td>
                                    <%-- <asp:Label runat="server" Text="" ID="lbConstruccionModal"></asp:Label>--%>
                                    <asp:DropDownList ID="ddlConstruccionModal" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" OnTextChanged="ddlConstruccionModal_TextChanged" DataSourceID ="dsConstruccion"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                     <asp:SqlDataSource ID="dsConstruccion" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlConstruccionSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Concepto:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlConcepto" runat="server" CssClass='form-control' Width="160px" DataSourceID="dsConceptos"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsConceptos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlConceptosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="UM:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlUM" runat="server" CssClass='form-control' Width="160px" DataSourceID="dsUm"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsUm" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlUmSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Precio:"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtPrecio" runat="server" CssClass='form-control'></asp:TextBox>
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" runat="server" TargetControlID="txtPrecio"
                                        FilterType="Numbers,Custom" ValidChars=",." />
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="Cantidad:"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtCantidad" runat="server" CssClass='form-control'></asp:TextBox>
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtCantidad"
                                        FilterType="Numbers,Custom" ValidChars=",." />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Material:"></asp:Label>
                                </td>
                                <td colspan="3">
                                    <asp:DropDownList ID="ddlMaterial" runat="server" CssClass='form-control' DataSourceID="dsMaterial"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsMaterial" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlMaterialesSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label runat="server" Text="Descripcion:"></asp:Label>
                                </td>
                                <td colspan="3">
                                    <asp:TextBox ID="txtDescripcion" runat="server" CssClass='form-control' TextMode="MultiLine"></asp:TextBox>
                                </td>
                            </tr>
                        </table>
                        <br />
                        <div style="text-align: center">
                            <asp:Button ID="btnAgregarDetalle" runat="server" Text="Agregar" CssClass="btn btn-success btn-sm" Width="100px" />
                            <asp:Button ID="btnEliminarDetalle" runat="server" Text="Eliminar" CssClass="btn btn-danger btn-sm" Width="100px" Visible="false" />
                            <asp:Button ID="btnModificarDetalle" runat="server" Text="Modificar" CssClass="btn btn-warning btn-sm" Width="100px" Visible="false" />
                            <asp:Button ID="btnCancelarSeleccion" runat="server" Text="Cancelar" CausesValidation="False"
                                CssClass="btn btn-primary btn-sm" Width="100px" Visible="false" />
                        </div>
                    </div>

                </div>
                <%--TERMINO PARA AGREGAR ITEMS--%>

                <%--PARA LA GRILLA PRINCIPAL--%>
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="Actas agregadas"></asp:Label>
                    </div>
                    <asp:GridView ID="gvActas" runat="server" AllowPaging="True" AutoGenerateColumns="False" CssClass="gvHeader"
                        DataSourceID="dsActas" EnableModelValidation="True" DataKeyNames="id" EmptyDataText="No ha agregado items aún">
                        <Columns>
                            <asp:CommandField AccessibleHeaderText="Seleccionar" SelectText="Seleccionar" ShowSelectButton="True" HeaderText="Seleccionar" />
                            <asp:BoundField DataField="id" HeaderText="Registro" InsertVisible="False" ReadOnly="True" SortExpression="concepto" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="concepto" HeaderText="Concepto" InsertVisible="False" ReadOnly="True" SortExpression="concepto" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="um" HeaderText="Unidad Medida" SortExpression="um" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            <asp:BoundField DataField="cantidad" HeaderText="Cantidad" SortExpression="cantidad" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            <asp:BoundField DataField="precio" HeaderText="Precio" SortExpression="precio" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            <asp:BoundField DataField="descripcion" HeaderText="Descripcion" SortExpression="descripcion" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            <asp:BoundField DataField="material" HeaderText="Material" SortExpression="material" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            <asp:BoundField DataField="construccion" HeaderText="Construcción" SortExpression="construccion" ItemStyle-HorizontalAlign="Left"></asp:BoundField>

                        </Columns>
                    </asp:GridView>
                    <asp:SqlDataSource ID="dsActas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="DetalleActasTempSelProc" SelectCommandType="StoredProcedure">
                        <SelectParameters>
                            <asp:Parameter Name="arquitecto" Type="String" />
                            <asp:Parameter Name="revisor" Type="String" />
                            <asp:Parameter Name="contratista" Type="String" />
                        </SelectParameters>
                    </asp:SqlDataSource>
                </div>
                <%--TERMINO LA GRILLA PRINCIPAL--%>
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="Opciones"></asp:Label>
                    </div>
                    <table>
                        <tr>
                            <td></td>
                            <td></td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" ID="lbComentarioFinal" Text="Comentario:"> </asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtComentariosFinales" runat="server" CssClass="form-control" Width="380px" TextMode="MultiLine"></asp:TextBox></p>
                            </td>
                        </tr>
                    </table>
                    <div style="text-align: center">
                        <asp:Button ID="btnRegistrarActa" runat="server" Text="Registrar" CssClass="btn btn-primary btn-sm" Width="100px" Visible="false" OnClientClick="return confirm('¿Está seguro que desea registrar información?');" CausesValidation="false" />
                        <asp:Button ID="btnCancelarActa" runat="server" Text="Cancelar" CausesValidation="False"
                            CssClass="btn btn-danger btn-sm" Width="100px" Visible="false" OnClientClick="return confirm('Toda la información registrada se perdera , ¿ desea continuar?');" />
                    </div>
                    <div  style="text-align: center">
                        <asp:Button ID="btnImprimir" runat="server" Text="Imprimir" CssClass="btn btn-info btn-sm" Width="100px" Visible="false" OnClientClick="return confirm('La impresion se abrira en una ventana emergente ,por favor continue');" CausesValidation="false" />
                    </div>
                </div>

            </asp:Panel>
            <asp:HiddenField ID="hfShowDetalle" runat="server" />
            <asp:HiddenField ID="hfHideDetalle" runat="server" />
            <cc1:ModalPopupExtender ID="mpDetalle" runat="server" BackgroundCssClass="FondoAplicacion"
                CancelControlID="hfHideDetalle" Enabled="true" PopupControlID="panelDetalle"
                TargetControlID="hfShowDetalle">
            </cc1:ModalPopupExtender>





        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnRegistrarActa" />
            <asp:PostBackTrigger ControlID="btnImprimir" />
        </Triggers>
    </asp:UpdatePanel>
</asp:Content>

