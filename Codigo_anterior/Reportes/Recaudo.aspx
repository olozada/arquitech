<%@ page title="" language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_Recaudo, App_Web_zfcqopj3" %>


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
                    <asp:Label runat="server" Text="Informe Recaudos"></asp:Label></small>
            </h1>

            <div class="panel panel-default">
                <div class="panel-heading">Opciones de busqueda</div>
                <div class="panel-body">
                    <CP:FiltroFechas runat="server" ID="FiltroFechas" />
                    <p style="text-align: center">
                        <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary" ValidationGroup="fecha" /></p>
                    <p style="text-align: center">
                        &nbsp;
                    </p>
                    <p style="text-align: center">
                        &nbsp;
                    </p>
                    <p style="text-align: center">
                        &nbsp;
                    </p>
                </div>
            </div>

            <div class="col-lg-12">
                <div class="col-lg-12">
                    <div class="panel panel-default">
                        <div class="panel-heading">Recaudo</div>
                        <div class="panel-body">
                            <div class="table-responsive ">
                                <asp:CheckBox ID="ChkNutresaCartera" runat="server" AutoPostBack="True" Text="Grupo Nutresa" />
                                <asp:GridView ID="gvCartera" runat="server" AllowSorting="True" AutoGenerateColumns="False"
                                    CssClass="gvHeader" DataKeyNames="cliente" DataSourceID="dsCartera"
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
                                <asp:Button ID="Button1" runat="server" CausesValidation="false" CssClass="btn btn-primary" OnClick="Button1_Click" Text="Exportar" />
                            </div>
                            <asp:SqlDataSource ID="dsCartera" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaCarteraResumenComparativoSelProc" SelectCommandType="StoredProcedure">
                                <SelectParameters>
                                    <asp:Parameter Name="nutresa" Type="Int32" />
                                    <asp:Parameter Name="fechainicial" Type="Int32" />
                                    <asp:Parameter Name="fechafinal" Type="Int32" />
                                </SelectParameters>
                            </asp:SqlDataSource>
                        </div>
                    </div>
                </div>
            </div>

        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="Button1" />
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

