<%@ control language="VB" autoeventwireup="false" inherits="Filtros_FiltrosUsuarios, App_Web_dhfbald2" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<div class=" table-responsive">
    <table style="margin: auto;">
        <tr>
            <td>
                <b>
                    <label>Fecha </label>
                </b>
            </td>
            <td>
                <asp:TextBox ID="txtFechaInicial" runat="server" Width="160px" CssClass='form-control'></asp:TextBox>
            </td>
            <td>
                <i class="fa fa-calendar" aria-hidden="true" id="Image1" runat="server"></i>
                <asp:RequiredFieldValidator ID="RequiredFieldValidator10" runat="server" ErrorMessage="Campo requerido"
                    ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaInicial"></asp:RequiredFieldValidator>
                <cc1:CalendarExtender ID="CalendarExtender1" runat="server" TargetControlID="txtFechaInicial"
                    PopupButtonID="Image1">
                </cc1:CalendarExtender>
            </td>
        </tr>
        <tr>
            <td>
                <b>
                    <label>Gerente regional </label>
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
                    <label>Jefe de ventas </label>
                </b>
            </td>
            <td colspan="2">
                <asp:DropDownList ID='ddlJefeVentas' runat='server' CssClass='form-control' Width="160px" DataSourceID="dsJefeVentas"
                    DataTextField="nombre" DataValueField="id" AutoPostBack="True">
                </asp:DropDownList>
                <asp:SqlDataSource ID="dsJefeVentas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                    SelectCommand="SupervisoresDdlSelProc" SelectCommandType="StoredProcedure">
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
                    <label>Vendedor</label>
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
                        <asp:ControlParameter ControlID="ddlJefeVentas" Name="Jefe de ventas" PropertyName="SelectedValue" Type="String" />
                    </SelectParameters>
                </asp:SqlDataSource>
            </td>
        </tr>
    </table>
</div>
