<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_ReporteActas, App_Web_wpwkx5nf" title="CELUWEB" %>

<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>


<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">

    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="Impresion de Actas"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
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
                                    <asp:Label runat="server" Text="Arquitecto:"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlArquitectos" runat="server" CssClass='form-control' Width="160px" AutoPostBack="True" DataSourceID="dsArquitectos"
                                        DataTextField="nombre" DataValueField="codigo">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="dsArquitectos" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                                        SelectCommand="ddlArquitectosSelProc" SelectCommandType="StoredProcedure"></asp:SqlDataSource>
                                </td>
                            </tr>
                        </table>
                        <table style="margin: auto;">
                            <tr>
                                <td colspan="3">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary" />
                                        <asp:Button ID="btnExportar" runat="server" Text="Exportar" CssClass="btn btn-primary" />
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="Resumen"></asp:Label>
                </div>

                <asp:Panel ID="Panel1" runat="server">
                    <h2 style="text-align: center;">
                        <asp:Label runat="server" Text="Consolidado"></asp:Label>
                    </h2>
                    <div class="table-responsive" style="overflow-x: scroll">
                        <asp:GridView ID="gvActas" runat="server" AutoGenerateColumns="False" Width="97%" EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA BUSQUEDA"
                            OnRowDataBound="gvActas_RowDataBound" ShowFooter="True" CssClass="gvHeader" DataSourceID="dsActas" EnableModelValidation="True" OnRowCommand="gvActas_RowCommand">
                            <Columns>
                                <asp:BoundField DataField="acta" HeaderText="N°Acta" SortExpression="acta" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="fechainicial" HeaderText="Fecha inicial" SortExpression="fechainicial" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="fechafinal" HeaderText="Fecha Final" SortExpression="fechafinal" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="contratista" HeaderText="Contratista" SortExpression="contratista" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="observacion" HeaderText="Observacion" SortExpression="Observacion" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="totalacta" HeaderText="Total" SortExpression="totalacta" ItemStyle-HorizontalAlign="Left" />
                                <asp:TemplateField HeaderText="Acta" SortExpression="Acta">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnImprimir" Text="Imprimir" runat="server" CommandArgument='<%# Eval("acta") %>' CommandName="imprimir" onClientClick=<%# string.Format("window.open('ImpresionActas.aspx?numerodoc={0}','_self');", Eval("acta")) %>/>
                                    </ItemTemplate>
                                </asp:TemplateField>

                            </Columns>
                            <FooterStyle CssClass="gvHeader-footer" />
                        </asp:GridView>
                    </div>
                    <asp:SqlDataSource ID="dsActas" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                        SelectCommand="ListaActasSelProc" SelectCommandType="StoredProcedure">
                        <SelectParameters>
                            <asp:Parameter Name="fechainicial" Type="String" />
                            <asp:Parameter Name="fechafinal" Type="String" />
                            <asp:Parameter Name="arquitecto" Type="String" />
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
