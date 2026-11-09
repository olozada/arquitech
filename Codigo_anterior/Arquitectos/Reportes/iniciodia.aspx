<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_iniciodia, App_Web_1pmgrmve" title="CELUWEB" %>

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
                <small> <asp:Label runat="server" Text="<%$ Resources:multi.language, hora_inicio_labores%>"></asp:Label></small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <CP:Filtros ID="filtros" runat="server" />
                        <table style="margin: auto;">
                            <tr>
                                <td colspan="3">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnAceptar" runat="server" Text="<%$ Resources:multi.language, aceptar%>" CssClass="btn btn-primary" />
                                        <asp:Button ID="btnExportar" runat="server" Text="<%$ Resources:multi.language, exportar%>" CssClass="btn btn-primary" />
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
                <asp:Panel ID="Panel1" runat="server" CssClass="panel">
                    <asp:GridView ID="gvhora" runat="server" AutoGenerateColumns="False" DataSourceID="dsListaHoraInicio"
                        CssClass="gvHeader">
                        <Columns>
                            <asp:BoundField DataField="CodVendedor" HeaderText="<%$ Resources:multi.language, codigo%>" SortExpression="CodVendedor" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="Vendedor" HeaderText="<%$ Resources:multi.language, vendedor%>" SortExpression="Vendedor" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="HoraInicio" HeaderText="<%$ Resources:multi.language, hora_inicio%>" ReadOnly="True" SortExpression="HoraInicio" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="primerVisita" HeaderText="<%$ Resources:multi.language, primera_visita%>" ReadOnly="True" SortExpression="primerVisita" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="horaFin" HeaderText="<%$ Resources:multi.language, hora_fin%>" ReadOnly="True" SortExpression="horaFin" ItemStyle-HorizontalAlign="Left" />
                        </Columns>
                    </asp:GridView>
                    <asp:SqlDataSource ID="dsListaHoraInicio" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                        SelectCommand="ListaInicioLaboresSelProc" SelectCommandType="StoredProcedure">
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
