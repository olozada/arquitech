<%@ page title="CELUWEB" language="C#" masterpagefile="~/menu.master" autoeventwireup="true" inherits="Reportes_ReporteHabeasData, App_Web_uu9hjelu" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="AjaxControlToolkit" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <AjaxControlToolkit:UpdatePanel ID="upPrincipal" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:Label ID="lblTitulo" runat="server" Text="REPORTE HABEAS DATA" CssClass="titulos"></asp:Label>
            <br />
            <br />
            <table class="panelCenter">
                <tr>
                    <td>
                        <CP:Filtros runat="server" ID="TCFiltrosNew" />
                    </td>
                </tr>
            </table>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    
                    <table style="margin: auto;">
                        <tr>
                            <td colspan="3">
                                <div style="text-align: center">
                                    <asp:Button ID="btnFiltrar" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:multi.language, filtrar%>" OnClick="btnFiltrar_Click" />
                                    &nbsp;<asp:Button ID="btnExcel" runat="server" CssClass="btn btn-primary" OnClick="btnExcel_Click" Text="EXCEL" />
                                    <asp:Button ID="btnGenerarFotos" runat="server" CssClass="btn btn-primary" Text="ZIP" ToolTip="Exportar Fotos en Zip" OnClick="btnGenerarFotos_Click" OnClientClick="return confirm('Para la generación del archivo zip se abrirá una nueva ventana, por favor tenga en cuenta que en algunos casos este proceso puede demorar muchos minutos, por lo tanto no intente cerrar la ventana emergente y espere a que salga el mensaje indicándole cómo descargar el archivo');"></asp:Button>

                                </div>
                            </td>
                        </tr>
                    </table>

                </div>
            </div>
          

            <br />
            <asp:GridView ID="gvDatos" runat="server" CssClass="gvHeader" DataKeyNames="numDoc" AutoGenerateColumns="False" DataSourceID="dsHabeasData" OnRowDataBound="gvDatos_RowDataBound" AllowPaging="True" PageSize="25" OnSelectedIndexChanged="gvDatos_SelectedIndexChanged">
                <Columns>
                    <asp:BoundField DataField="numDoc" HeaderText="<%$ Resources:multi.language, Documento%>" ReadOnly="True" SortExpression="numDoc" />
                    <asp:BoundField DataField="codigoCliente" HeaderText="<%$ Resources:multi.language, codigo_cliente%>" SortExpression="codigoCliente" />
                    <asp:BoundField DataField="nit" HeaderText="<%$ Resources:multi.language, Nit%>" SortExpression="nit" />
                    <asp:BoundField DataField="nombre" HeaderText="<%$ Resources:multi.language, Nombre%>" SortExpression="nombre" />
                    <asp:BoundField DataField="ciudad" HeaderText="<%$ Resources:multi.language, Ciudad%>" SortExpression="ciudad" />
                    <asp:BoundField DataField="email" HeaderText="e-mail" SortExpression="email" />
                    <asp:BoundField DataField="autorizo" HeaderText="<%$ Resources:multi.language, autorizado%>" SortExpression="autorizo" />
                    <asp:BoundField DataField="nuevo" HeaderText="<%$ Resources:multi.language, nuevo%>" SortExpression="nuevo" />
                    <asp:BoundField DataField="observacion" HeaderText="<%$ Resources:multi.language, observacion%>" SortExpression="observacion" />
                    <asp:BoundField DataField="codvendedor" HeaderText="<%$ Resources:multi.language, Codigo_vendedor%>" SortExpression="codvendedor" />
                    <asp:BoundField DataField="vendedor" HeaderText="<%$ Resources:multi.language, Vendedor%>" SortExpression="vendedor" />
                    <asp:BoundField DataField="fechaServidor" HeaderText="<%$ Resources:multi.language, fecha_registro%>" SortExpression="fechaServidor" />
                    <asp:CommandField HeaderText="<%$ Resources:multi.language, foto%>" SelectText="Ver foto" ShowSelectButton="True" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="dsHabeasData" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ReporteHabeasDataSelProc" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:Parameter Name="fechaInicial" Type="String" />
                    <asp:Parameter Name="fechaFinal" Type="String" />
                    <asp:Parameter Name="usuario" Type="String" />
                    <asp:Parameter Name="tipoUsuario" Type="String" />
                    <asp:Parameter Name="gerenteRegional" Type="String" />
                    <asp:Parameter Name="jefeVenta" Type="String" />
                    <asp:Parameter Name="vendedor" Type="String" />
                </SelectParameters>
            </asp:SqlDataSource>
            <br />
            <asp:Panel ID="panelResumen" runat="server" BackColor="White" CssClass="modalPopUp"
                Height="420px" Width="600px">
                <br />
                <div style="text-align: center">
                    <img runat="server" id="image" style="height: 330px; width: 520px;" />
                </div>
                <asp:Button runat="server" Text="<%$ Resources:multi.language, cerrar%>" ID="btnCerrarPopup" OnClick="btnCerrarPopup_Click" CssClass="btn btn-primary" />
            </asp:Panel>

            <asp:HiddenField ID="hfShowResumen" runat="server" />
            <asp:HiddenField ID="hfHideResumen" runat="server" />
            <cc1:ModalPopupExtender ID="mpPanelResumen" runat="server" PopupControlID="panelResumen"
                TargetControlID="hfShowResumen" BackgroundCssClass="FondoAplicacion" CancelControlID="hfHideResumen">
            </cc1:ModalPopupExtender>
        </ContentTemplate>
        <Triggers>
            <AjaxControlToolkit:PostBackTrigger ControlID="btnExcel"></AjaxControlToolkit:PostBackTrigger>
            <AjaxControlToolkit:PostBackTrigger ControlID="btnGenerarFotos"></AjaxControlToolkit:PostBackTrigger>
        </Triggers>
        <%--</asp:UpdatePanel>--%>
    </AjaxControlToolkit:UpdatePanel>
</asp:Content>

