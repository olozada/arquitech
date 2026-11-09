<%@ page title="CELUWEB" language="C#" masterpagefile="~/menu.master" autoeventwireup="true" inherits="Reportes_ReporteProductosCompetencia, App_Web_uu9hjelu" %>


<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="updatepanel" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, unidades_x_cliente%>"></asp:Label>

                </small>
            </h1>
            <div class="panel panel-default">
                <div class="panel-body">

                    <table style="margin: auto;">
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, fecha_inicial%>"> </asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtFechaInicial" runat="server" Width="160px" CssClass='form-control'></asp:TextBox>
                            </td>
                            <td>
                                <i class="fa fa-calendar" aria-hidden="true" id="Image1" runat="server"></i>
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator10" runat="server" ErrorMessage="Campo requerido"
                                    ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaInicial"></asp:RequiredFieldValidator>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, fecha_final%>"> </asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtFechaFinal" runat="server" Width="160px" CssClass='form-control'></asp:TextBox>
                            </td>
                            <td>
                                <i class="fa fa-calendar" aria-hidden="true" id="imgb1" runat="server"></i>
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Campo requerido"
                                    ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaFinal"></asp:RequiredFieldValidator>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, gerente_regional%>"> </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:DropDownList ID="ddlgerenteRegional" runat="server" CssClass='form-control' Width="160px"
                                    AutoPostBack="True" DataSourceID="dsgerenteRegional"
                                    DataTextField="gerenteRegional" DataValueField="id">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, jefe_ventas%>"> </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:DropDownList ID='ddlJefeVentas' runat='server' CssClass='form-control' Width="160px"
                                    DataSourceID="dsJefeVentas"
                                    DataTextField="nombre" DataValueField="id" AutoPostBack="True">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, vendedor%>"> </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:DropDownList ID="ddlVendedores" runat="server" CssClass='form-control' Width="160px"
                                    DataSourceID="dsVendedores" DataTextField="nombre" DataValueField="codigo" AutoPostBack="true">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, cliente%>"> </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:DropDownList ID="ddlClientes" runat="server" Width="160px" CssClass="form-control" AutoPostBack="True" DataSourceID="SqlClientes" DataTextField="NOMBRE" DataValueField="CODIGO">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, producto%>"> </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:DropDownList ID="ddlProducto" runat="server" CssClass='form-control' Width="160px" DataSourceID="dsProductos" DataTextField="producto" DataValueField="id"></asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <div style="text-align: center">
                                    <asp:Button ID="btnAceptar" runat="server" Text="<%$ Resources:multi.language, aceptar%>" OnClick="btnAceptar_Click" CssClass="btn btn-primary" />
                                    <asp:Button ID="btnExportar" runat="server" Text="<%$ Resources:multi.language, exportar%>" OnClick="btnExportar_Click" CssClass="btn btn-primary" />
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>
            </div>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, resumen%>"> </asp:Label>
                </div>
                <div class="panel-body">
                    <div class="table-responsive">
                        <asp:GridView ID="gvInforme" runat="server" CssClass="gvHeader" AutoGenerateColumns="False"
                            DataSourceID="SqlInforme" EnableModelValidation="True" Width="100%" OnRowDataBound="gvInforme_RowDataBound"
                            ShowFooter="true">
                            <FooterStyle CssClass="gvHeader-footer" />
                            <Columns>
                                <asp:BoundField DataField="codigoVendedor" HeaderText="<%$ Resources:multi.language, codigo_vendedor%>" ReadOnly="True" SortExpression="codigoVendedor" />
                                <asp:BoundField DataField="codigoCliente" HeaderText="<%$ Resources:multi.language, codigo_cliente%>" ReadOnly="True" SortExpression="codigoCliente" />
                                <asp:BoundField DataField="nombreCliente" HeaderText="<%$ Resources:multi.language, nombre_cliente%>" ReadOnly="True" SortExpression="nombreCliente" />
                                <asp:BoundField DataField="codigoRef" HeaderText="<%$ Resources:multi.language, codigo_producto%>" SortExpression="codigoRef" />
                                <asp:BoundField DataField="descripcion" HeaderText="<%$ Resources:multi.language, descripcion%>" ReadOnly="True" SortExpression="descripcion" />
                                <asp:BoundField DataField="precio" HeaderText="<%$ Resources:multi.language, precio%>" ReadOnly="True" SortExpression="precio" DataFormatString="{0:C}">
                                    <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField DataField="cantidad" HeaderText="<%$ Resources:multi.language, cantidad%>" ReadOnly="True" SortExpression="cantidad">
                                    <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField DataField="total" HeaderText="<%$ Resources:multi.language, total%>" ReadOnly="True" SortExpression="total" DataFormatString="{0:C}">
                                    <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
            <asp:SqlDataSource ID="SqlInforme" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                SelectCommand="UnidadesxClienteSelProc" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:Parameter Name="FechaInicial" Type="String" />
                    <asp:Parameter Name="FechaFinal" Type="String" />
                    <asp:ControlParameter ControlID="ddlClientes" Name="CodigoCliente" PropertyName="SelectedValue" Type="String" />
                    <asp:ControlParameter ControlID="ddlProducto" Name="CodigoProducto" PropertyName="SelectedValue" Type="String" />
                </SelectParameters>
            </asp:SqlDataSource>
            <asp:SqlDataSource ID="SqlClientes" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                SelectCommand="ddlClientesVendedor" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:ControlParameter ControlID="ddlVendedores" PropertyName="selectedValue" Name="Vendedor" Type="String" />
                </SelectParameters>
            </asp:SqlDataSource>
            <%-- --------------------------%>
            <cc1:CalendarExtender ID="CalendarExtender1" runat="server" TargetControlID="txtFechaInicial"
                PopupButtonID="Image1">
            </cc1:CalendarExtender>

            <cc1:CalendarExtender ID="CalendarExtender2" runat="server" TargetControlID="txtFechaFinal"
                PopupButtonID="imgb1">
            </cc1:CalendarExtender>
            <asp:SqlDataSource ID="dsgerenteRegional" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                SelectCommand="GerenteRegionalDdlSelProc" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:SessionParameter SessionField="tipo" Name="tipoUsuario" Type="String" />
                    <asp:SessionParameter SessionField="codigoUsuario" Name="codUsuario" Type="String" />
                </SelectParameters>
            </asp:SqlDataSource>
            <asp:SqlDataSource ID="dsJefeVentas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                SelectCommand="JefeVentasDdlSelProc" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:SessionParameter SessionField="tipo" Name="tipoUsuario" Type="String" />
                    <asp:SessionParameter SessionField="codigoUsuario" Name="codUsuario" Type="String" />
                    <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                </SelectParameters>
            </asp:SqlDataSource>
            <asp:SqlDataSource ID="dsVendedores" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                SelectCommand="VendedoresDdlSelProc" SelectCommandType="StoredProcedure">
                <SelectParameters>
                    <asp:SessionParameter SessionField="codigoUsuario" Name="usuario" Type="String" />
                    <asp:SessionParameter SessionField="tipo" Name="tipoUsuario" Type="String" />
                    <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                    <asp:ControlParameter ControlID="ddlJefeVentas" Name="jefeVenta" PropertyName="SelectedValue" Type="String" />
                    <asp:Parameter Name="todos" Type="String" DefaultValue="0" />
                </SelectParameters>
            </asp:SqlDataSource>
            <asp:SqlDataSource ID="dsProductos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ProductosDdl"
                SelectCommandType="StoredProcedure"></asp:SqlDataSource>

        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnExportar" />
        </Triggers>
    </asp:UpdatePanel>
</asp:Content>


