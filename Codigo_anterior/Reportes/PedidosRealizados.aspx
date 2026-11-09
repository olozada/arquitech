<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_PedidosRealizados, App_Web_zfcqopj3" title="CELUWEB" %>

<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>


<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">

    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">
                <small>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, Pedidos_realizados%>"></asp:Label>
                </small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Lista Instructoress</div>--%>
                <div class="panel-body">
                    <div class=" table-responsive">
                        <CP:Filtros ID="filtros" runat="server" />
                        <table style="margin: auto;">
                            <tr>
                                <td colspan="3">
                                    <div style="text-align: center">
                                        <asp:Button ID="btnAceptar" runat="server" Text="<%$ Resources:multi.language, Aceptar%>" CssClass="btn btn-primary" />
                                        <asp:Button ID="btnExportar" runat="server" Text="<%$ Resources:multi.language, Exportar%>" CssClass="btn btn-primary" />
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading">
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, resumen%>"></asp:Label>
                </div>

                <%--Div con la gráfica de barras--%>
                <div>
                    <div id="container2" class="col-sm-6 col-xs-12">
                    </div>
                    <%--Div con la gráfica de pastel--%>
                    <div id="container" class="col-sm-6 col-xs-12">
                    </div>
                </div>
                <hr />
                <asp:Panel ID="Panel1" runat="server">
                    <h2 style="text-align: center;">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, consolidado%>"></asp:Label>
                    </h2>
                    <div class="table-responsive">
                        <asp:GridView ID="gvPedidosRealizados" runat="server" AutoGenerateColumns="False" Width="97%" EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA BUSQUEDA"
                            OnRowDataBound="gvPedidosRealizados_RowDataBound" ShowFooter="True" CssClass="gvHeader" DataSourceID="dsPedidosRealizados" EnableModelValidation="True">
                            <Columns>
                                <asp:BoundField DataField="codVendedor" HeaderText="<%$ Resources:multi.language, codigo%>" SortExpression="codVendedor" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="vendedor" HeaderText="<%$ Resources:multi.language, vendedor%>" ReadOnly="True" SortExpression="vendedor" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="fechaIni" HeaderText="<%$ Resources:multi.language, fecha_inicial%>" ReadOnly="True" SortExpression="fechaIni" ItemStyle-HorizontalAlign="Center" />
                                <asp:BoundField DataField="fechaFin" HeaderText="<%$ Resources:multi.language, fecha_final%>" ReadOnly="True" SortExpression="fechaFin" ItemStyle-HorizontalAlign="Center" />
                                <asp:BoundField DataField="visitasObjetivo" HeaderText="<%$ Resources:multi.language, visitas_objetivo%>" ReadOnly="True" SortExpression="visitasObjetivo" ItemStyle-HorizontalAlign="right" />
                                <asp:BoundField DataField="visitasRealizadas" HeaderText="<%$ Resources:multi.language, visitas_realizadas%>" ReadOnly="True" SortExpression="visitasRealizadas" ItemStyle-HorizontalAlign="right" />
                                <asp:BoundField DataField="pedidosrealizados" HeaderText="<%$ Resources:multi.language, pedidos_realizados%>" ReadOnly="True" SortExpression="pedidosrealizados" ItemStyle-HorizontalAlign="right" />
                                <asp:BoundField DataField="visitasCompra" HeaderText="<%$ Resources:multi.language, visitas_compra%>" ReadOnly="True" SortExpression="visitasCompra" ItemStyle-HorizontalAlign="right" />
                                <asp:BoundField DataField="efectividadVisita" HeaderText="<%$ Resources:multi.language, efectividad_visitas%>" ReadOnly="True" SortExpression="efectividadVisita" ItemStyle-HorizontalAlign="right" DataFormatString="{0:p}" />
                                <asp:BoundField DataField="nivelServicio" HeaderText="<%$ Resources:multi.language, nivel_servicio%>" ReadOnly="True" SortExpression="nivelServicio" ItemStyle-HorizontalAlign="right" DataFormatString="{0:p}" />
                                <asp:BoundField DataField="ventaTotal" HeaderText="<%$ Resources:multi.language, venta_total%>" ReadOnly="True" SortExpression="ventaTotal" ItemStyle-HorizontalAlign="right" DataFormatString="{0:c}" />
                                <asp:BoundField DataField="presupuesto" HeaderText="<%$ Resources:multi.language, presupuesto%>" ReadOnly="True" SortExpression="presupuesto" ItemStyle-HorizontalAlign="right" DataFormatString="{0:c}" />
                                <asp:BoundField DataField="cumplimiento" HeaderText="<%$ Resources:multi.language, cumplimiento%>" ReadOnly="True" SortExpression="cumplimiento" ItemStyle-HorizontalAlign="right" DataFormatString="{0:p}" />
                            </Columns>
                            <FooterStyle CssClass="gvHeader-footer" />
                        </asp:GridView>
                    </div>
                    <asp:SqlDataSource ID="dsPedidosRealizados" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>"
                        SelectCommand="ListaPediddosRealizadosSelProc" SelectCommandType="StoredProcedure">
                        <SelectParameters>
                            <asp:Parameter Name="fechainicial" Type="String" />
                            <asp:Parameter Name="fechafinal" Type="String" />
                            <asp:Parameter Name="usuario" Type="String" />
                            <asp:Parameter Name="tipoUsuario" Type="String" />
                            <asp:Parameter Name="gerenteRegional" Type="String" />
                            <asp:Parameter Name="jefeVenta" Type="String" />
                            <asp:Parameter Name="vendedor" Type="String" />
                        </SelectParameters>
                    </asp:SqlDataSource>
                </asp:Panel>
            </div>
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
            console.log(datos);
            graficaPastel(datos);
            graficaBarra(datos);
        }

        function graficaPastel(datos) {

            var motivos = datos[0];
            var cantidades = [];

            for (i = 0; i < datos[1].length; i++) {
                cantidades.push(parseFloat(datos[1][i]));
            }

            var numerosFinales = [];

            for (var i = 0; i < motivos.length; i++) {
                var obj1 = [];
                obj1.push(motivos[i]);
                obj1.push(parseFloat(cantidades[i]));
                numerosFinales.push(obj1);
            };

            $('#container').highcharts({
                chart: {
                    plotBackgroundColor: null,
                    plotBorderWidth: null,
                    plotShadow: false,
                    type: 'pie'
                },
                title: {
                    text: '<%= Resources.multi.language.Distribucion_ventas.ToString()%>',
                },
                exporting: {
                    filename: 'Novedades Pedidos',//Define el nombre del archivo para exportar
                    enabled: true,//Define si muestra o no el botón de exportar la gráfica generada
                    sourceWidth: 1200,
                    sourceHeight: 760,
                    //scale: 2 (default)
                    chartOptions:
                    {
                        subtitle: null
                    }
                },
                tooltip: {
                    pointFormat: '{series.name}: <b>{point.percentage:.1f}%</b>'
                },
                plotOptions: {
                    pie: {
                        allowPointSelect: true,
                        cursor: 'pointer',
                        dataLabels: {
                            enabled: false
                        },
                        showInLegend: true
                    }
                },
                series: [{
                    type: 'pie',
                    name: '<%= Resources.multi.language.porcentaje_venta.ToString()%>',
                    data: numerosFinales

                }]
            });
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

                console.log('---------');
                console.log(itemSerie);

                series2.push(itemSerie);
            }

            console.log(series2);

            $('#container2').highcharts({
                chart: {
                    type: 'column'
                },
                title: {
                    text: '<%= Resources.multi.language.Pedidos_x_vendedor.ToString()%>'
                },
                subtitle: {
                    text: ''
                },
                xAxis: {
                    categories: '',
                    crosshair: true,
                    title: {
                        text: '<%= Resources.multi.language.Vendedor.ToString()%>'
                    }
                },
                yAxis: {
                    min: 0,
                    title: {
                        text: '<%= Resources.multi.language.Cantidad.ToString()%>'
                    }
                },
                tooltip: {
                    headerFormat: '<span style="font-size:10px">{point.key}</span><table>',
                    pointFormat: '<tr><td style="color:{series.color};padding:0">{series.name}: </td>' +
                    '<td style="padding:0"><b>{point.y:.2f}' + '</b></td></tr>',
                    footerFormat: '</table>',
                    shared: true,
                    useHTML: true
                },
                plotOptions: {
                    column: {
                        pointPadding: 0.2,
                        borderWidth: 0
                    },
                    series: {
                        borderWidth: 0,
                        dataLabels: {
                            // rotation: -90,
                            enabled: true,
                            format: '{point.y:.0f}'
                        }
                    }
                },
                series: series2
            });
        }
    </script>
</asp:Content>
