<%@ page title="" language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_ReportePortafolioProducto, App_Web_1pmgrmve" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="FiltrosLinea" Src="~/Filtros/FiltrosUsuariosLinea.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="udpanel" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, portafolio_cliente%>"></asp:Label></small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    <div class=" table-responsive">

                        <div class=" table-responsive">
                            <table style="margin: auto;">

                                <tr>
                                    <td>
                                        <b>
                                            <asp:Label runat="server" Text="<%$ Resources:multi.language, gerente_regional%>"></asp:Label>
                                        </b>
                                    </td>
                                    <td colspan="2">
                                        <asp:DropDownList ID="ddlgerenteRegional" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsgerenteRegional"
                                            DataTextField="gerenteRegional" DataValueField="id">
                                        </asp:DropDownList>
                                        <asp:SqlDataSource ID="dsgerenteRegional" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                            SelectCommand="GerenteRegionalDdlSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="tipoUsuario" Type="String" />
                                                <asp:Parameter Name="codUsuario" Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="auto-style1">
                                        <b>
                                            <asp:Label runat="server" Text="<%$ Resources:multi.language, jefe_ventas%>"></asp:Label>
                                        </b>
                                    </td>
                                    <td colspan="2" class="auto-style1">
                                        <asp:DropDownList ID='ddlJefeVentas' runat='server' CssClass='form-control' Width="160px" DataSourceID="dsJefeVentas"
                                            DataTextField="nombre" DataValueField="id" AutoPostBack="True">
                                        </asp:DropDownList>
                                        <asp:SqlDataSource ID="dsJefeVentas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                            SelectCommand="JefeVentasDdlSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="tipoUsuario" Type="String" />
                                                <asp:Parameter Name="codUsuario" Type="String" />
                                                <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <b>
                                            <asp:Label runat="server" Text="<%$ Resources:multi.language, vendedor%>"></asp:Label>
                                        </b>
                                    </td>
                                    <td colspan="2">
                                        <asp:DropDownList ID="ddlVendedores" AutoPostBack="True" runat="server" CssClass='form-control' Width="160px" DataSourceID="dsVendedores" DataTextField="nombre" DataValueField="codigo">
                                        </asp:DropDownList>
                                        <asp:SqlDataSource ID="dsVendedores" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="VendedoresDdlSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="usuario" Type="String" />
                                                <asp:Parameter Name="tipoUsuario" Type="String" />
                                                <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                                                <asp:ControlParameter ControlID="ddlJefeVentas" Name="Jefeventa" PropertyName="SelectedValue" Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <b>
                                            <asp:Label runat="server" Text="<%$ Resources:multi.language, linea%>"></asp:Label>
                                        </b>
                                    </td>
                                    <td colspan="2">
                                        <asp:DropDownList ID="ddlLineaVenta" runat="server" CssClass="form-control" DataSourceID="dsLineaProducto" DataTextField="linea" DataValueField="codLinea" Width="160px">
                                        </asp:DropDownList>
                                        <asp:SqlDataSource ID="dsLineaProducto" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="LineasProductosDdlSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                    </td>
                                </tr>

                                <tr>
                                    <td>
                                        <b>
                                            <asp:Label runat="server" Text="<%$ Resources:multi.language, cliente%>"></asp:Label>
                                        </b>
                                    </td>
                                    <td colspan="2">
                                        <asp:DropDownList ID="ddlCliente" runat="server" CssClass="form-control" AppendDataBoundItems="true" DataSourceID="dsCliente" DataTextField="nombre" DataValueField="codigo" Width="160px">
                                        </asp:DropDownList>
                                        <asp:SqlDataSource ID="dsCliente" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ddlClienteSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="usuario" Type="String" />
                                                <asp:Parameter Name="tipoUsuario" Type="String" />
                                                <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                                                <asp:ControlParameter ControlID="ddlJefeVentas" Name="jefeVenta" PropertyName="SelectedValue" Type="String" />
                                                <asp:ControlParameter ControlID="ddlVendedores" Name="Vendedor" PropertyName="SelectedValue" Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </td>
                                </tr>
                            </table>
                        </div>

                        <div class="table-responsive ">
                            <table style="margin: auto; width: 268px">
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
            </div>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, resumen%>"></asp:Label>
                </div>
                <div class="table-responsive">
                    <asp:GridView ID="gvPortafolioCliente" runat="server" CssClass="gvHeader" AutoGenerateColumns="False" DataSourceID="dsPortafolioCliente"
                        EnableModelValidation="True" DataKeyNames="codigoLin" Width="90%" AllowPaging="true">
                        <Columns>
                            <%--<asp:CommandField HeaderText="Ver detalle" SelectText="Ver" ShowSelectButton="True" ItemStyle-HorizontalAlign="Center" />--%>
                            <asp:BoundField DataField="nombre" HeaderText="<%$ Resources:multi.language, nombre%>" SortExpression="nombre" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="codProducto" HeaderText="<%$ Resources:multi.language, codigo_producto%>" SortExpression="codProducto" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="descripcion" HeaderText="<%$ Resources:multi.language, descripcion%>" SortExpression="descripcion" ReadOnly="True" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="iva" HeaderText="<%$ Resources:multi.language, iva%>" ReadOnly="True" SortExpression="iva" ItemStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="precio" HeaderText="<%$ Resources:multi.language, precio%>" ReadOnly="True" SortExpression="precio" DataFormatString="{0:c2}" ItemStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="codigoLin" HeaderText="<%$ Resources:multi.language, linea%>" ReadOnly="True" SortExpression="codigoLin" ItemStyle-HorizontalAlign="Left" />
                            <asp:BoundField DataField="nombreLin" HeaderText="<%$ Resources:multi.language, nombre_linea%>" ReadOnly="True" SortExpression="nombreLin" ItemStyle-HorizontalAlign="Left" />
                        </Columns>
                        <FooterStyle Font-Bold="true" />
                    </asp:GridView>
                </div>
                <asp:SqlDataSource ID="dsPortafolioCliente" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaPortafolioCliente" SelectCommandType="StoredProcedure">
                    <SelectParameters>
                        <asp:Parameter Name="usuario" Type="String" />
                        <asp:Parameter Name="tipoUsuario" Type="String" />
                        <asp:Parameter Name="gerenteRegional" Type="String" />
                        <asp:Parameter Name="Jefeventa" Type="String" />
                        <asp:Parameter Name="vendedor" Type="String" />
                        <asp:Parameter Name="cliente" Type="String" />
                        <asp:Parameter Name="linea" Type="String" />
                    </SelectParameters>
                </asp:SqlDataSource>
            </div>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnExportar" />

        </Triggers>
    </asp:UpdatePanel>
</asp:Content>

