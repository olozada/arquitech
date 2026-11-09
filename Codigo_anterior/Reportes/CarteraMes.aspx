<%@ page title="" language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_CarteraMes, App_Web_zfcqopj3" %>



<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>
<%@ Register Src="~/Filtros/FiltroFechas.ascx" TagPrefix="CP" TagName="FiltroFechas" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">

                <small>
                    <asp:Label runat="server" Text="Informe Pago Mes"></asp:Label></small>
            </h1>


            <div class="panel panel-default">


                <%--Div con la gráfica de barras--%>
                <%--<div class="col-sm-12">--%>
                <div class=" table-responsive">
                    <div id="container2">
                    </div>
                </div>
                <%--</div>--%>
                <div class="panel panel-default">
                    <%--<div class="panel-heading">Seleccionar cliente</div>--%>
                    <div class="panel-body">
                        <div class=" table-responsive">
                            <CP:FiltroFechas runat="server" ID="FiltroFechas" />
                            <table style="margin: auto;">
                                <tr>
                                    <td colspan="3">
                                        <div style="text-align: center">
                                            <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary" ValidationGroup="fecha" />

                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </div>
                </div>

                <asp:Panel ID="Panel1" runat="server" CssClass="panel panel-default">
                    <h2 style="text-align: center;">
                        <asp:Label runat="server" Text="Consolidado"></asp:Label>
                    </h2>
                    <div class="table-responsive">
                        <asp:CheckBox ID="chkNutresa" runat="server" AutoPostBack="True" Text="Grupo Nutresa" />
                        <asp:GridView ID="gvPedidosVendedor" runat="server" AutoGenerateColumns="False" EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA"
                            Width="100%" Font-Size="12px" CssClass="gvHeader" DataSourceID="dsPedidosVendedor" EnableModelValidation="True"
                            OnRowDataBound="gvPedidosVendedor_RowDataBound" ShowFooter="True" DataKeyNames="nombre" AllowSorting="True">
                            <Columns>
                                <asp:CommandField HeaderText="ver" SelectText="Ver" ShowSelectButton="True" />
                                <asp:BoundField DataField="nombre" HeaderText="Nombre" ItemStyle-HorizontalAlign="Left" SortExpression="cliente">
                                    <ItemStyle HorizontalAlign="Left" />
                                </asp:BoundField>
                                <asp:BoundField DataField="fecha" HeaderText="Fecha" ItemStyle-HorizontalAlign="Right" SortExpression="total">
                                    <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>
                                <asp:BoundField DataField="valor" HeaderText="Valor" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="Right" SortExpression="total">
                                    <ItemStyle HorizontalAlign="Right" />
                                </asp:BoundField>

                            </Columns>
                            <FooterStyle CssClass="gvHeader-footer" />
                        </asp:GridView>
                        <asp:Button ID="btnExportar" runat="server" Text="Exportar" CssClass="btn btn-primary" CausesValidation="false" />
                    </div>
                    <asp:SqlDataSource ID="dsPedidosVendedor" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaCarteraResumenMesSelProc" SelectCommandType="StoredProcedure">
                        <SelectParameters>
                            <asp:Parameter Name="nutresa" Type="String" />
                            <asp:Parameter Name="fi" Type="String" />
                            <asp:Parameter Name="ff" Type="String" />
                        </SelectParameters>
                    </asp:SqlDataSource>
                </asp:Panel>
            </div>

            <asp:Panel ID="panelResumen" runat="server" CssClass="modalPopUp">
                <div>
                    <asp:Button ID="bCloseResumen" runat="server" CausesValidation="false" CssClass="buttonPopUp" />
                </div>
                <div style="text-align: center">
                    <asp:Label ID="lblNumdoc" runat="server" Font-Bold="true" Font-Size="Large"></asp:Label>
                </div>
                <div class="panel panel-default">
                    <%--<div class="panel-heading">Información detallada<asp:Label ID="lblNumdoc" runat="server" Font-Bold="true" Font-Size="Large"></asp:Label></div>--%>
                    
                        <div class="panel panel-default">
                            <div class="panel-body">
                                <%-- <h5 style="margin-top: 0px; margin-bottom: 5px;">
                                        <asp:Label ID="label2" runat="server" Text="<%$ Resources:multi.language, detalle_pedido%>"></asp:Label>
                                    </h5>--%>
                                <div class="table-responsive">
                                    
                                        <asp:GridView ID="gvdetalle" runat="server" AutoGenerateColumns="False" CssClass="gvHeader" DataSourceID="dsDetalle" EnableModelValidation="True"
                                            ShowFooter="true" OnRowDataBound="gvdetalle_RowDataBound" Width="100%" DataKeyNames="nombre">
                                            <Columns>
                                                
                                                <asp:BoundField DataField="nombre" HeaderText="Nombre" ReadOnly="True" SortExpression="Nombre" />                                                
                                                <asp:BoundField DataField="fecha" HeaderText="Fecha" SortExpression="fecha" ItemStyle-HorizontalAlign="right" />
                                               <asp:BoundField DataField="valor" HeaderText="Valor" SortExpression="Valor" ItemStyle-HorizontalAlign="right" DataFormatString="$ {0:N0}" />
                                            </Columns>
                                            <FooterStyle CssClass="gvHeader-footer" />
                                        </asp:GridView>
                                        <asp:SqlDataSource ID="dsDetalle" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="DetallePagoSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="cliente" Type="String" />
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

    <%--Script para las gráficas hechas con highcharts--%>
    <script type="text/javascript">

        //Método que recibe los datos obtenidos por el SP con los campos a graficar
        //y llama a los métodos encargados de graficar
        function iniciar(datos) {
            console.log(datos);
            graficaBarra(datos);
        }

        //Gráfica de barras

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
                    text: '<% =  Resources.multi.language.top_5_vendedores.ToString()%>' //'Top 5 Vendedores con más ventas'
                },
                subtitle: {
                    text: ''
                },
                xAxis: {
                    categories: '',
                    crosshair: true
                    ,
                    title: {
                        text: '<% =  Resources.multi.language.Vendedor.ToString()%>'
                    }
                },
                yAxis: {
                    min: 0,
                    title: {
                        text: '<% =  Resources.multi.language.Pedidos.ToString()%>'
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
    <script type="text/javascript">
        function initMap(latitud, longitud) {
            console.log(latitud);
            console.log(longitud);
            var uluru = { lat: parseFloat(latitud), lng: parseFloat(longitud) };

            var map = new google.maps.Map(document.getElementById('map'), {
                zoom: 16,
                center: uluru
            });
            var marker = new google.maps.Marker({
                position: uluru,
                map: map
            });
        }
    </script>
    <%-- <script async defer
        src="https://maps.googleapis.com/maps/api/js?key=AIzaSyDf1s74m2sYm-tiCglRIi1pl3fRZ8XUYf0&callback=initMap">
    </script>--%>
    <script type='text/javascript' src='https://maps.googleapis.com/maps/api/js?key=AIzaSyDRL0ag9-lF_XxR9IS2BbahTlEh-IrrGBM'></script>
    <style>
        #map {
            height: 280px;
            width: 100%;
        }
    </style>
</asp:Content>

