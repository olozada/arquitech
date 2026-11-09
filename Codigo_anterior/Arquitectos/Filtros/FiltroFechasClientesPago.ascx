<%@ control language="VB" autoeventwireup="false" inherits="Filtros_FiltroFechasClientesPago, App_Web_fis9ndvb" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<div class=" table-responsive">
    <table style="margin: auto;">
        <tr>
            <td>
                <b>
                    <asp:Label runat="server" Text="Fecha Inicial" ID="l2"></asp:Label>
                </b>
            </td>
            <td>
                <asp:TextBox ID="txtFechaInicial" runat="server" Width="160px" CssClass='form-control' ValidationGroup="fecha"></asp:TextBox>
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
                    <asp:Label runat="server" Text="Fecha Final" ID="l1"></asp:Label>
                </b>
            </td>
            <td>
                <asp:TextBox ID="txtFechaFinal" runat="server" Width="160px" CssClass='form-control' ValidationGroup="fecha"></asp:TextBox>
            </td>
            <td>
                <i class="fa fa-calendar" aria-hidden="true" id="imgb1" runat="server"></i>
                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Campo requerido"
                    ForeColor="Red" Font-Size="9px" ControlToValidate="txtFechaFinal"></asp:RequiredFieldValidator>

                <cc1:CalendarExtender ID="CalendarExtender2" runat="server" TargetControlID="txtFechaFinal"
                    PopupButtonID="imgb1">
                </cc1:CalendarExtender>
            </td>
        </tr>
        <tr>
            <td>
                <b>
                    <asp:Label runat="server" Text="cliente :" ID="Label1"></asp:Label>
                </b>
            </td>
            <td>
                <asp:DropDownList ID="ddlClientes" runat="server" AutoPostBack="false" DataSourceID="dsClientes" DataTextField="Descripcion" DataValueField="Codigo" CssClass="form-control" Width="160px">
                    <asp:ListItem Value="-1">--SELECCIONE--</asp:ListItem>
                </asp:DropDownList>
                <asp:SqlDataSource ID="dsClientes" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ddlClienteSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
            </td>
        </tr>

           <tr>
            <td>
                <b>
                    <asp:Label runat="server" Text="cliente pago:" ID="Label2"></asp:Label>
                </b>
            </td>
            <td>
                <asp:DropDownList ID="ddlClientesPagos" runat="server" AutoPostBack="false" DataSourceID="dsClientesPagos" DataTextField="Descripcion" DataValueField="Codigo" CssClass="form-control" Width="160px">
                    <asp:ListItem Value="-1">--SELECCIONE--</asp:ListItem>
                </asp:DropDownList>
                <asp:SqlDataSource ID="dsClientesPagos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ddlClientePagosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
            </td>
        </tr>

    </table>
</div>
