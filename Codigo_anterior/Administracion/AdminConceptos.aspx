<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Administracion_AdminConceptos, App_Web_d4eebhyk" title="CELUWEB" %>

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
                    <asp:Label runat="server" Text="Administracion de Conceptos"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="Concepto"></asp:Label>
                </div>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <table style="margin: auto;">
                            <tr>
                                <td rowspan="4">
                                   <%-- <asp:Image ID="Image1" runat="server" ImageUrl="~/Styles/images/usuarioV.png" />--%>
                                </td>
                                <td>
                                    <asp:Label runat="server" Text="Descripcion"></asp:Label>:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDescripcion" runat="server" CssClass='form-control'></asp:TextBox>
                                </td>
                                <td>
                                    <asp:RequiredFieldValidator ID="validator" runat="server" ControlToValidate="txtDescripcion"
                                        ErrorMessage=" * "></asp:RequiredFieldValidator>
                                </td>
                            </tr>


                             <tr>
                                <td>
                                    <asp:Label runat="server" Text="Unidad"></asp:Label>:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtUnidad" runat="server" CssClass='form-control'></asp:TextBox>
                                </td>
                                <td>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtUnidad"
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
                     <asp:Label runat="server" Text="Lista de Conceptos"></asp:Label>
                </div>
                <asp:GridView ID="gvConceptos" runat="server" AllowPaging="True" AutoGenerateColumns="False" CssClass="gvHeader"
                    DataSourceID="dsConceptos" EnableModelValidation="True" DataKeyNames="codigo">
                    <Columns>
                        <asp:CommandField AccessibleHeaderText="Seleccionar" SelectText="Seleccionar" ShowSelectButton="True" HeaderText="Seleccionar" />
                        <asp:BoundField DataField="codigo" HeaderText="Codigo" InsertVisible="False" ReadOnly="True" SortExpression="codigo" ItemStyle-HorizontalAlign="Left" />
                        <asp:BoundField DataField="nombre" HeaderText="Descripcion" SortExpression="nombre" ItemStyle-HorizontalAlign="Left"></asp:BoundField>                        
                        <asp:BoundField DataField="unidad" HeaderText="Unidad" SortExpression="unidad" ItemStyle-HorizontalAlign="Left"></asp:BoundField>                        
                    </Columns>
                </asp:GridView>
                <asp:SqlDataSource ID="dsConceptos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="AdminConceptosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
            </div>
        </ContentTemplate>       
        <Triggers>
            <asp:PostBackTrigger ControlID="btnCancelar"/>
        </Triggers>
        
    </asp:UpdatePanel>
    
</asp:Content>
