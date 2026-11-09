<%@ page title="" language="C#" masterpagefile="~/menu.master" autoeventwireup="true" inherits="Reportes_ReporteRegVisita, App_Web_uu9hjelu" %>


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
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, informe_recaudos_vendedor%>"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    <CP:Filtros ID="filtros" runat="server" />
                    <table style="margin: auto;">
                        <tr>
                            <td colspan="3">
                                <div style="text-align: center">
                                    <asp:Button ID="btnAceptar" runat="server" Text="<%$ Resources:multi.language, aceptar%>" CssClass="btn btn-primary" OnClick="btnAceptar_Click" />
                                    <asp:Button ID="btnExportar" runat="server" Text="<%$ Resources:multi.language, exportar%>" CssClass="btn btn-primary" />
                                </div>
                            </td>
                        </tr>
                    </table>

                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, consolidado%>"></asp:Label>
                </div>
                <div class="panel-body">
                    <div class="table-responsive">
                        <asp:GridView ID="gvDatos" runat="server" CssClass="gvHeader " AutoGenerateColumns="False"
                           ShowFooter="True" DataSourceID="dsClientesVisitados" OnRowDataBound="gvDatos_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="#" HeaderText="#" ReadOnly="True" SortExpression="#" />
                                <asp:BoundField DataField="CodVendedor" HeaderText="<%$ Resources:multi.language, Codigo_vendedor%>" SortExpression="CodVendedor" />
                                <asp:BoundField DataField="Vendedor" HeaderText="<%$ Resources:multi.language, Vendedor%>" SortExpression="Vendedor" />
                                <asp:BoundField DataField="CodigoCliente" HeaderText="<%$ Resources:multi.language, Codigo_cliente%>" SortExpression="CodigoCliente" />
                                <asp:BoundField DataField="Cliente" HeaderText="<%$ Resources:multi.language, cliente%>" SortExpression="Cliente" />
                                <asp:BoundField DataField="Direccion" HeaderText="<%$ Resources:multi.language, Direccion%>" SortExpression="Direccion" />
                                <asp:BoundField DataField="Barrio" HeaderText="<%$ Resources:multi.language, Barrio%>" SortExpression="Barrio" />
                                <asp:BoundField DataField="Telefono" HeaderText="<%$ Resources:multi.language, Telefono%>" SortExpression="Telefono" />
                                <asp:BoundField DataField="MotivoVisita" HeaderText="<%$ Resources:multi.language, RazonVisita%>" SortExpression="MotivoVisita" />
                              <%--  <asp:BoundField DataField="DetalleActividad" HeaderText="DetalleActividad" SortExpression="DetalleActividad" />
                                <asp:BoundField DataField="Motivo" HeaderText="Motivo" SortExpression="Motivo" />--%>
                                <asp:BoundField DataField="FechaInicial" HeaderText="<%$ Resources:multi.language, Fecha_inicial%>" SortExpression="FechaInicial" />
                                <asp:BoundField DataField="FechaFinal" HeaderText="<%$ Resources:multi.language, Fecha_final%>" SortExpression="FechaFinal" />
                                <asp:BoundField DataField="TiempoVisita" HeaderText="<%$ Resources:multi.language, TiempoVisita%>" ReadOnly="True" SortExpression="TiempoVisita" />
                                <asp:BoundField DataField="TiempoRecorrido" HeaderText="<%$ Resources:multi.language, TiempoRecorrido%>" ReadOnly="True" SortExpression="TiempoRecorrido" />
                                <asp:BoundField DataField="FechaServidor" HeaderText="<%$ Resources:multi.language, fecha_registro%>" SortExpression="FechaServidor" />
                                <asp:BoundField DataField="Rango" HeaderText="<%$ Resources:multi.language, Rango%>" SortExpression="Rango" />
                                <asp:BoundField DataField="RangoTolerancia" HeaderText="<%$ Resources:multi.language, RangoTolerancia%>" SortExpression="RangoTolerancia" />
                                <asp:BoundField DataField="Diferencia" HeaderText="<%$ Resources:multi.language, Diferencia%>" SortExpression="Diferencia" />
                                <asp:BoundField DataField="Latitud" HeaderText="<%$ Resources:multi.language, latitud%>" SortExpression="Latitud" />
                                <asp:BoundField DataField="Longitud" HeaderText="<%$ Resources:multi.language, Longitud%>" SortExpression="Longitud" />
                            </Columns>
                            <FooterStyle BackColor="#476786" />
                        </asp:GridView>
                        <asp:SqlDataSource ID="dsClientesVisitados" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ReporteRegVisitaSelProc" SelectCommandType="StoredProcedure">
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
                    </div>
                </div>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnExportar" />
        </Triggers>
    </asp:UpdatePanel>
</asp:Content>

