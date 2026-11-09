<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_ReportePQR, App_Web_zfcqopj3" title="CELUWEB" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">

                <small>
                    <asp:Label runat="server" Text="Informe PQR"></asp:Label></small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Seleccionar cliente</div>--%>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <CP:Filtros ID="filtros" runat="server" />
                        <table style="margin: auto;">
                            <tr>
                                <td colspan="3">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnAceptar" runat="server" Text="<%$ Resources:multi.language, Aceptar%>" CssClass="btn btn-primary" />
                                        <asp:Button ID="btnExportar" runat="server" Text="<%$ Resources:multi.language, Exportar%>" CssClass="btn btn-primary" />
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, resumen%>"></asp:Label>
                </div>

                <%--Div con la gráfica de barras--%>
                <%--<div class="col-sm-12">--%>
                <div class=" table-responsive">
                    <div id="container2">
                    </div>
                </div>
                <%--</div>--%>

                <asp:Panel ID="Panel1" runat="server" CssClass="panel panel-default">
                    <h2 style="text-align: center;">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, consolidado%>"></asp:Label>
                    </h2>
                    <div class="table-responsive">
                        <asp:GridView ID="gvPedidosVendedor" runat="server" AutoGenerateColumns="False" EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA"
                            Width="100%" Font-Size="12px" CssClass="gvHeader" DataSourceID="dsPedidosVendedor" EnableModelValidation="True">
                            <Columns>
                                <asp:BoundField DataField="Numerodoc" HeaderText="Numerodoc" SortExpression="Numerodoc" />
                                <asp:BoundField DataField="Vendedor" HeaderText="Codigo Vendedor" SortExpression="Vendedor" />
                                <asp:BoundField DataField="NVendedor" HeaderText="Vendedor" SortExpression="NVendedor" />
                                <asp:BoundField DataField="Cliente" HeaderText="Codigo Cliente" SortExpression="Cliente" />
                                <asp:BoundField DataField="Nombre" HeaderText="Cliente" SortExpression="Nombre" />
                                <asp:BoundField DataField="descripcion" HeaderText="Motivo Pqr" SortExpression="descripcion" />
                                <asp:BoundField DataField="Observacion" HeaderText="Observacion" SortExpression="Observacion" />
                            </Columns>
                        </asp:GridView>
                    </div>
                    <asp:SqlDataSource ID="dsPedidosVendedor" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ReportePQRSelProc" SelectCommandType="StoredProcedure">
                        <SelectParameters>
                            <asp:Parameter Name="fechainicial" Type="String" />
                            <asp:Parameter Name="fechafinal" Type="String" />
                            <asp:Parameter Name="usuario" Type="String" />
                            <asp:Parameter Name="tipoUsuario" Type="String" />
                            <asp:Parameter Name="gerenteRegional" Type="String" />
                            <asp:Parameter Name="jefeVenta" Type="String" />
                            <asp:Parameter Name="vendedor" Type="String" />
                        </SelectParameters>
                    </asp:SqlDataSource>
                </asp:Panel>
            </div>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnExportar" />
        </Triggers>
    </asp:UpdatePanel>

    
</asp:Content>
