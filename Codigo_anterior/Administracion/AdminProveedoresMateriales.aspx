<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Administracion_AdminProveedoresMateriales, App_Web_d4eebhyk" title="CELUWEB" %>

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
                    <asp:Label runat="server" Text="Administración de Unidades de Medida"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="Unidad de medida"></asp:Label>
                </div>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <table style="margin: auto;">
                            <tr>
                                <td>
                                    <asp:Label runat="server" ID="lbProveedor" Text="Proveedor:"> </asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList runat="server" DataSourceID="dsProveedores" DataValueField="codigo" DataTextField="nombre" ID="ddlProveedores" CssClass="form-control" AutoPostBack="true" OnTextChanged="ddlProveedores_TextChanged"></asp:DropDownList>
                                    <asp:SqlDataSource ID="dsProveedores" runat="server" SelectCommand="ddlProveedoresSelProc" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </div>


            <div class="col-lg-12">
                <div class="col-md-5">
                    <div class="panel panel-default">
                        <div class="panel-heading">
                            <asp:Label runat="server" Text="Materiales agregados"></asp:Label>
                        </div>
                        <asp:GridView ID="gvAgregados" runat="server" AllowPaging="True" AutoGenerateColumns="False" CssClass="gvHeader"
                            DataSourceID="dsAgregados" EnableModelValidation="True" DataKeyNames="codigoproducto">
                            <Columns>
                                 <asp:TemplateField>
                                    <HeaderTemplate>
                                        <asp:CheckBox ID="ckbSelectT" runat="server" Text="Todos" onClick="javascript:selectAllGridView(this);" />
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="ckbSelect" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="codigoproducto" HeaderText="Codigo" InsertVisible="False" ReadOnly="True" SortExpression="codigo" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="descripcion" HeaderText="Descripcion" InsertVisible="False" ReadOnly="True" SortExpression="descripcion" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="valor" HeaderText="Precio" SortExpression="nombre" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                            </Columns>
                        </asp:GridView>
                        <asp:SqlDataSource ID="dsAgregados" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="MaterialesAgregadosSelProc" SelectCommandType="StoredProcedure">
                            <SelectParameters>
                                <asp:Parameter Name="proveedor" Type="String" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                    </div>
                </div>
                <div class="col-md-2">
                    <p style="text-align: center">
                        <asp:Button ID="btnQuitar" runat="server" CausesValidation="False" Text="Quitar"
                            CssClass="btn btn-danger btn-sm" Width="100px" />
                    </p>
                    <p style="text-align: center">
                        &nbsp
                    </p>
                    <p style="text-align: center">
                        <asp:Button ID="btnAgregar" runat="server" Text="Agregar" CausesValidation="False"
                            CssClass="btn btn-primary btn-sm" Width="100px" />
                    </p>
                </div>
                <div class="col-md-5">
                    <div class="panel panel-default">
                        <div class="panel-heading">
                            <asp:Label runat="server" Text="Materiales sin agregar"></asp:Label>
                        </div>
                        <asp:GridView ID="gvSinAgregar" runat="server" AllowPaging="True" AutoGenerateColumns="False" CssClass="gvHeader"
                            DataSourceID="dsSinAgregar" EnableModelValidation="True" DataKeyNames="codigoproducto">
                            <Columns>
                                <asp:TemplateField>
                                    <HeaderTemplate>
                                        <asp:CheckBox ID="ckbSelectT" runat="server" Text="Todos" onClick="javascript:selectAllGridView(this);" />
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="ckbSelect" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:BoundField DataField="codigoproducto" HeaderText="Codigo" InsertVisible="False" ReadOnly="True" SortExpression="codigo" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="descripcion" HeaderText="Descripcion" SortExpression="descripcion" ItemStyle-HorizontalAlign="Left"></asp:BoundField>
                                <asp:TemplateField HeaderText="Precio">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtCantidad" runat="server" Width="50px"></asp:TextBox>
                                    </ItemTemplate>
                                </asp:TemplateField>

                            </Columns>
                        </asp:GridView>
                        <asp:SqlDataSource ID="dsSinAgregar" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="Materiales_Sin_agregar_Selproc" SelectCommandType="StoredProcedure">
                            <SelectParameters>
                                <asp:Parameter Name="proveedor" Type="String" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                    </div>
                </div>
            </div>


        </ContentTemplate>
        <%-- <Triggers>
            <asp:PostBackTrigger ControlID="btnCancelar" />
        </Triggers>--%>
    </asp:UpdatePanel>

</asp:Content>
