<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Administracion_AdminArquitectos, App_Web_ssaqtgzb" title="CELUWEB" %>

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
                    <asp:Label runat="server" Text="Administracion de Arquitectos"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="Perfíl Arquitecto"></asp:Label>
                </div>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <table style="margin: auto;">
                            <tr>
                                <td rowspan="4">
                                    <asp:Image ID="Image1" runat="server" ImageUrl="~/Styles/images/usuarioV.png" />
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="Nombre"></asp:Label>:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtNombre" runat="server" CssClass='form-control'></asp:TextBox>
                                </td>
                                <td>
                                    <asp:RequiredFieldValidator ID="validator" runat="server" ControlToValidate="txtNombre"
                                        ErrorMessage=" * "></asp:RequiredFieldValidator>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="4">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnCrear" runat="server" Text="Crear" CssClass="btn btn-success btn-sm" Width="100px" />
                                        <asp:Button ID="btnModificar" runat="server" CausesValidation="False" Text="Modificar"
                                            Visible="False" CssClass="btn btn-success btn-sm" Width="100px" />
                                        <asp:Button ID="btnEliminar" runat="server" CausesValidation="False" Text="Eliminar"
                                            Visible="False" CssClass="btn btn-danger btn-sm" Width="100px" />
                                        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CausesValidation="False"
                                            CssClass="btn btn-primary btn-sm" Width="100px" />
                                    </div>
                                </td>
                            </tr>

                        </table>
                    </div>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading">
                     <asp:Label runat="server" Text="Lista de Arquitectos"></asp:Label>
                </div>
                <asp:GridView ID="gvArquitectos" runat="server" AllowPaging="True" AutoGenerateColumns="False" CssClass="gvHeader"
                    DataSourceID="dsArquitectos" EnableModelValidation="True" DataKeyNames="nombre">
                    <Columns>
                        <asp:CommandField AccessibleHeaderText="Seleccionar" SelectText="Seleccionar" ShowSelectButton="True" HeaderText="Seleccionar" />
                        <asp:BoundField DataField="codigo" HeaderText="Codigo" InsertVisible="False" ReadOnly="True" SortExpression="codigo" ItemStyle-HorizontalAlign="Left" />
                        <asp:BoundField DataField="nombre" HeaderText="Nombre" SortExpression="nombre" ItemStyle-HorizontalAlign="Left"></asp:BoundField>                        
                    </Columns>
                </asp:GridView>
                <asp:SqlDataSource ID="dsArquitectos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="AdminArquitectosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
            </div>
        </ContentTemplate>       
        <Triggers>
            <asp:PostBackTrigger ControlID="btnCancelar"/>
        </Triggers>
        
    </asp:UpdatePanel>
    
</asp:Content>
