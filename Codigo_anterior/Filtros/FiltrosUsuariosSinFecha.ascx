<%@ control language="VB" autoeventwireup="false" inherits="Filtros_FiltrosUsuariosSinFecha, App_Web_dhfbald2" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
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
            <td>
                <b>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, jefe_ventas%>"></asp:Label>
                </b>
            </td>
            <td colspan="2">
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
                <asp:DropDownList ID="ddlVendedores" runat="server" CssClass='form-control' Width="160px" DataSourceID="dsVendedores" DataTextField="nombre" DataValueField="codigo">
                </asp:DropDownList>
                <asp:SqlDataSource ID="dsVendedores" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="VendedoresDdlSelProc" SelectCommandType="StoredProcedure">
                    <SelectParameters>
                        <asp:Parameter Name="usuario" Type="String" />
                        <asp:Parameter Name="tipoUsuario" Type="String" />
                        <asp:ControlParameter ControlID="ddlgerenteRegional" Name="gerenteRegional" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="ddlJefeVentas" Name="jefeVenta" PropertyName="SelectedValue" Type="String" />
                    </SelectParameters>
                </asp:SqlDataSource>
            </td>
        </tr>
    </table>
</div>
