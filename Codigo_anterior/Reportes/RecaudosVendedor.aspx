<%@ page title="CELUWEB" language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_RecaudosVendedor, App_Web_zfcqopj3" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/Filtrofechas.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, informe_recaudos_vendedor%>"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    <CP:Filtros ID="filtros" runat="server" />
                    <table style="margin: auto;">
                        <caption>

                            <tr>
                                <td colspan="3">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnAceptar" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:multi.language, aceptar%>" />
                                        <asp:Button ID="btnExportar" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:multi.language, exportar%>" />
                                    </div>
                                </td>
                            </tr>
                        </caption>
                    </table>
                </div>
            </div>

            <hr />
            <asp:RadioButton ID="RbTodos" runat="server" GroupName="Grupo1" Text="Todos" AutoPostBack="true" OnCheckedChanged="RbTodos_CheckedChanged" />
            <asp:RadioButton ID="RbGrupoNutresa" runat="server" GroupName="Grupo1" Text="Grupo Nutresa" OnCheckedChanged="RbGrupoNutresa_CheckedChanged" AutoPostBack="true" />
            <asp:RadioButton ID="RbOperacion" runat="server" GroupName="Grupo1" Text="Clientes operación" AutoPostBack="true" OnCheckedChanged="RbOperacion_CheckedChanged" />
            <asp:RadioButton ID="RbNoOperacion" runat="server" GroupName="Grupo1" Text="No operacón" AutoPostBack="true" OnCheckedChanged="RbNoOperacion_CheckedChanged" />
            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label ID="Graficas" runat="server" Text="Graficas"></asp:Label>
                </div>
                <div class="panel-body">
                    <div id="container2">
                    </div>
                </div>
            </div>
            <asp:Panel runat="server" ID="panelconsolidado" Visible="true">


                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, consolidado%>"></asp:Label>
                    </div>
                    <div class="panel-body">
                        <div class="table-responsive">

                            <asp:GridView ID="gvCartera" runat="server" AllowSorting="True" AutoGenerateColumns="False"
                                CssClass="gvHeader" DataKeyNames="cliente" DataSourceID="dsListaRecaudosVendedor"
                                EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA" EnableModelValidation="True"
                                Font-Size="12px" OnRowDataBound="gvCartera_RowDataBound" ShowFooter="True" Width="100%">
                                <Columns>
                                    <%--<asp:CommandField HeaderText="ver" SelectText="Ver" ShowSelectButton="True" />--%>
                                    <%--<asp:BoundField DataField="factura" HeaderText="NºFactura" ItemStyle-HorizontalAlign="Left" SortExpression="factura" />--%>
                                    <asp:BoundField DataField="cliente" HeaderText="Cliente" ItemStyle-HorizontalAlign="Left" SortExpression="cliente" />
                                    <asp:BoundField DataField="total" DataFormatString="{0:C0}" HeaderText="Valor" ItemStyle-HorizontalAlign="Right" SortExpression="total" />
                                    <asp:BoundField DataField="fvenc" HeaderText="Fecha" ItemStyle-HorizontalAlign="Center" SortExpression="fvenc" />
                                </Columns>
                                <FooterStyle CssClass="gvHeader-footer" />
                            </asp:GridView>
                        </div>
                        <asp:SqlDataSource ID="dsListaRecaudosVendedor" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaCarteraResumenComparativoSelProc" SelectCommandType="StoredProcedure">
                            <SelectParameters>
                                <asp:Parameter Name="fechainicial" Type="String" />
                                <asp:Parameter Name="fechafinal" Type="String" />
                                <asp:Parameter Name="nutresa" Type="String" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                    </div>
                </div>
            </asp:Panel>
            <asp:Panel ID="panelResumen" runat="server" CssClass="modalPopUp">
                <div>
                    <asp:Button ID="bCloseResumen" runat="server" CausesValidation="false" CssClass="buttonPopUp" />
                </div>
                <div style="text-align: center">
                    <asp:Label ID="lblNumdoc" runat="server" Font-Bold="true" Font-Size="Large"></asp:Label>
                </div>
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, Informacion_cliente%>"></asp:Label>

                    </div>
                    <div class="panel-body">
                        <div class="table-responsive">
                            <asp:GridView ID="gvCliente" runat="server" AutoGenerateColumns="False" CssClass="gvHeader" DataSourceID="dsDatosCliente"
                                EnableModelValidation="True" Width="100%">
                                <Columns>
                                    <asp:BoundField DataField="codigo" HeaderText="<%$ Resources:multi.language, codigo%>" ReadOnly="True" SortExpression="codigo" />
                                    <asp:BoundField DataField="nombre" HeaderText="<%$ Resources:multi.language, nombre%>" SortExpression="nombre" />
                                    <asp:BoundField DataField="razonsocial" HeaderText="<%$ Resources:multi.language, razon_social%>" ReadOnly="True" SortExpression="razonsocial" />
                                    <asp:BoundField DataField="nit" HeaderText="<%$ Resources:multi.language, nit%>" ReadOnly="True" SortExpression="nit" />
                                    <asp:BoundField DataField="ciudad" HeaderText="<%$ Resources:multi.language, ciudad%>" SortExpression="ciudad" />
                                    <asp:BoundField DataField="direccion" HeaderText="<%$ Resources:multi.language, direccion%>" SortExpression="direccion" />
                                </Columns>
                            </asp:GridView>
                        </div>
                        <asp:SqlDataSource ID="dsDatosCliente" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="DatosClienteSelProc" SelectCommandType="StoredProcedure">
                            <SelectParameters>
                                <asp:Parameter Name="codigoCliente" Type="String" />
                                <asp:Parameter Name="nuevo" Type="String" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                    </div>
                </div>
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, descripcion_recaudo%>"></asp:Label>
                    </div>
                    <div class="panel-body">
                        <div class="panel panel-default col-sm-6">
                            <h3>
                                <small>
                                    <asp:Label runat="server" Text="<%$ Resources:multi.language, Facturas%>"></asp:Label>
                                </small>
                            </h3>
                            <div class="table-responsive">
                                <asp:GridView ID="gvDetalleRecaudo" runat="server" AutoGenerateColumns="False" DataSourceID="dsListaDetalleRecaudo" EnableModelValidation="True"
                                    EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA" ShowFooter="True" OnRowDataBound="gvDetalleRecaudo_RowDataBound"
                                    Width="100%" Font-Size="12px" CssClass="gvHeader">
                                    <Columns>
                                        <asp:BoundField DataField="factura" HeaderText="<%$ Resources:multi.language, Factura%>" SortExpression="factura" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField DataField="tipo" HeaderText="<%$ Resources:multi.language, tipo%>" SortExpression="tipo" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField DataField="valor" HeaderText="<%$ Resources:multi.language, valor%>" SortExpression="valor" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:c}" />
                                    </Columns>
                                    <FooterStyle CssClass="gvHeader-footer" />
                                </asp:GridView>
                                <asp:SqlDataSource ID="dsListaDetalleRecaudo" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaDetalleRecaudoSelProc" SelectCommandType="StoredProcedure">
                                    <SelectParameters>
                                        <asp:Parameter Name="documento" Type="String" />
                                    </SelectParameters>
                                </asp:SqlDataSource>
                            </div>
                        </div>
                        <div class="panel panel-default col-sm-6">
                            <h3>
                                <small>
                                    <asp:Label runat="server" Text="<%$ Resources:multi.language, forma_pago%>"></asp:Label>
                                </small>
                            </h3>
                            <div class="table-responsive">
                                <asp:GridView ID="gvFormasPago" runat="server" AutoGenerateColumns="False" DataSourceID="dsListaFormaPagoRecaudo" EnableModelValidation="True"
                                    EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA" ShowFooter="true" OnRowDataBound="gvFormasPago_RowDataBound"
                                    Width="100%" Font-Size="12px" CssClass="gvHeader">
                                    <Columns>
                                        <asp:BoundField DataField="tipoPago" HeaderText="<%$ Resources:multi.language, tipo_pago%>" ReadOnly="True" SortExpression="tipoPago" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField DataField="consignacion" HeaderText="<%$ Resources:multi.language, consignacion%>" ReadOnly="True" SortExpression="consignacion" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField DataField="valor" HeaderText="<%$ Resources:multi.language, valor%>" ReadOnly="True" SortExpression="valor" ItemStyle-HorizontalAlign="Right" DataFormatString="{0:c}" />
                                    </Columns>
                                    <FooterStyle CssClass="gvHeader-footer" />
                                </asp:GridView>

                                <asp:SqlDataSource ID="dsListaFormaPagoRecaudo" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaFormaPagoRecaudoSelProc" SelectCommandType="StoredProcedure">
                                    <SelectParameters>
                                        <asp:Parameter Name="documento" Type="String" />
                                    </SelectParameters>
                                </asp:SqlDataSource>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <asp:HiddenField ID="hfShowResumen" runat="server" />
            <asp:HiddenField ID="hfHideResumen" runat="server" />
            <cc1:ModalPopupExtender ID="mpPanelResumen" runat="server" PopupControlID="panelResumen"
                TargetControlID="hfShowResumen" BackgroundCssClass="FondoModal" CancelControlID="hfHideResumen">
            </cc1:ModalPopupExtender>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="btnExportar" />
        </Triggers>
    </asp:UpdatePanel>

    <script type="text/javascript">
        var _fechaInicial;
        var _fechaFinal;
        var _canal;
        var _vendedor;

        function iniciar(datos) {
            graficaBarra(datos);
        }

        function graficaBarra(datos) {
            var series2 = [];

            // metodo para crear las series como se muestra en el codigo arriba que esta comentado
            for (var i = 0; i < datos[0].length; i++) {

                var _name = datos[0][i]
                var _data = [];

                _data.push(parseFloat(datos[1][i]));

                var itemSerie = {
                    name: _name,
                    data: _data

                };
                series2.push(itemSerie);
            }

            Highcharts.setOptions({
                lang: {
                    thousandsSep: '.'
                }
            });

            $('#container2').highcharts({
                chart: {
                    type: 'column'
                    //,
                    //options3d: {
                    //    enabled: true,
                    //    alpha: 15,
                    //    beta: 13
                    //}
                },
                title: {
                    text: 'Ventas por Semana'
                },
                subtitle: {
                    text: ''
                },
                xAxis: {
                    categories: '',
                    crosshair: true,
                    title: {
                        text: 'Semana'
                    }
                },
                yAxis: {
                    min: 0,
                    title: {
                        text: 'Total'
                    }
                },
                plotOptions: {
                    column: {
                        pointPadding: 0.2,
                        borderWidth: 0,
                        depth: 30
                    },
                    series: {
                        borderWidth: 0,
                        dataLabels: {
                            // rotation: -90,

                            enabled: true,
                            format: '{point.y:,.0f}'
                            // format: '{point.y:.1f}'
                            //formatter: function () {
                            //    return Highcharts.numberFormat(this.y, 2);
                            //}
                        }
                    },
                    lang: {
                        thousandsSep: '.'
                    }

                },
                series: series2
            });

        }
    </script>
</asp:Content>

