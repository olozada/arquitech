<%@ page language="VB" masterpagefile="~/menu.master" autoeventwireup="false" inherits="Reportes_PedidosVendedor, App_Web_zfcqopj3" title="CELUWEB" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register TagPrefix="CP" TagName="Filtros" Src="~/Filtros/FiltrosUsuarios.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="Server">
    <asp:UpdatePanel ID="up1" runat="server">
        <ContentTemplate>
            <div class="panel-heading-icon">
                <i class="fa fa-line-chart " style="text-align: center;"></i>
            </div>
            <h1 class="page-header">

                <small>
                    <asp:Label runat="server" Text="<%$ Resources:multi.language, informe_pedidos_vendedor%>"></asp:Label></small>
            </h1>
            <div class="panel panel-default">
                <%--<div class="panel-heading">Seleccionar cliente</div>--%>
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
                <%--<div class="col-sm-12">--%>
                <div class=" table-responsive">
                    <div id="container2">
                    </div>
                </div>
                <%--</div>--%>

                <asp:Panel ID="Panel1" runat="server" CssClass="panel panel-default">
                    <h2 style="text-align: center;">
                        <asp:Label runat="server" Text="<%$ Resources:multi.language, consolidado%>"></asp:Label>
                    </h2>
                    <table style="margin: auto;">
                        <tr>
                            <td>
                                <asp:Label runat="server" Text="<%$ Resources:multi.language, valor_minimo%>"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtValorMinimo" runat="server" CssClass="form-control" Width="115px"></asp:TextBox>
                                <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" runat="server" TargetControlID="txtValorMinimo"
                                    FilterType="Numbers" />
                            </td>
                            <td>
                                <asp:RequiredFieldValidator ID="rfvTxtValorMinimo" runat="server" ErrorMessage="Campo requerido"
                                    ForeColor="Red" Font-Size="9px" ControlToValidate="txtValorMinimo" ValidationGroup="valorminimo"></asp:RequiredFieldValidator>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <div style="text-align: center;">
                                    <asp:Button ID="btnValorMinimo" runat="server" Text="<%$ Resources:multi.language, filtrar%>"
                                        CssClass="btn btn-success btn-xs" ValidationGroup="valorminimo" />
                                </div>
                            </td>
                        </tr>
                    </table>
                    <div class="table-responsive">
                        <asp:GridView ID="gvPedidosVendedor" runat="server" AutoGenerateColumns="False" EmptyDataText="NO SE ENCONTRARON RESULTADOS PARA ESTA  BUSQUEDA"
                            Width="100%" Font-Size="12px" CssClass="gvHeader" DataSourceID="dsPedidosVendedor" EnableModelValidation="True"
                            OnRowDataBound="gvPedidosVendedor_RowDataBound" ShowFooter="True" DataKeyNames="numerodoc">
                            <Columns>
                                <asp:CommandField HeaderText="<%$ Resources:multi.language, ver_detalle%>" SelectText="<%$ Resources:multi.language, ver%>" ShowSelectButton="True" />
                                <asp:BoundField DataField="sincronizado" HeaderText="<%$ Resources:multi.language, sincronizado%>" />
                                <asp:BoundField DataField="numerodoc" HeaderText="<%$ Resources:multi.language, documento%>" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="fecha" HeaderText="<%$ Resources:multi.language, fecha%>" />
                                <asp:BoundField DataField="codigocliente" HeaderText="<%$ Resources:multi.language, codigo%>" ReadOnly="True" />
                                <asp:BoundField DataField="cliente" HeaderText="<%$ Resources:multi.language, cliente%>" ItemStyle-HorizontalAlign="Left" />
                                <asp:BoundField DataField="vendedor" HeaderText="<%$ Resources:multi.language, vendedor%>" />
                                <asp:BoundField DataField="montofact" HeaderText="<%$ Resources:multi.language, valor_neto%>" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="Right" />
                                <asp:BoundField DataField="montofactbruto" HeaderText="<%$ Resources:multi.language, valor_bruto%>" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="Right" />
                                <asp:BoundField DataField="nuevo" HeaderText="<%$ Resources:multi.language, cliente_nuevo%>" />
                            </Columns>
                            <FooterStyle CssClass="gvHeader-footer" />
                        </asp:GridView>
                    </div>
                    <asp:SqlDataSource ID="dsPedidosVendedor" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="ListaPedidosVendedorSelProc" SelectCommandType="StoredProcedure">
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

            <asp:Panel ID="panelResumen" runat="server" CssClass="modalPopUp">
                <div>
                    <asp:Button ID="bCloseResumen" runat="server" CausesValidation="false" CssClass="buttonPopUp" />
                </div>
                <div style="text-align: center">
                    <asp:Label ID="lblNumdoc" runat="server" Font-Bold="true" Font-Size="Large"></asp:Label>
                </div>
                <div class="panel panel-default">
                    <%--<div class="panel-heading">Información detallada<asp:Label ID="lblNumdoc" runat="server" Font-Bold="true" Font-Size="Large"></asp:Label></div>--%>
                    <div class="panel-body">
                        <div class="col-sm-12 col-xs-12">
                            <div class="panel panel-default">
                                <div class="panel-body">
                                    <h5 style="margin-top: 0px; margin-bottom: 5px;">
                                        <asp:Label ID="label1" runat="server" Text="<%$ Resources:multi.language, cliente%>"></asp:Label>
                                    </h5>
                                    <div class="table-responsive">
                                        <asp:GridView ID="gvCliente" runat="server" AutoGenerateColumns="False" CssClass="gvHeader" DataSourceID="dsDatosCliente"
                                            EnableModelValidation="True" Width="98%">
                                            <Columns>
                                                <asp:BoundField DataField="codigo" HeaderText="<%$ Resources:multi.language, codigo%>" ReadOnly="True" SortExpression="codigo" />
                                                <asp:BoundField DataField="nombre" HeaderText="<%$ Resources:multi.language, nombre%>" SortExpression="nombre" />
                                                <asp:BoundField DataField="razonsocial" HeaderText="<%$ Resources:multi.language, razon_social%>" ReadOnly="True" SortExpression="razonsocial" />
                                                <asp:BoundField DataField="nit" HeaderText="<%$ Resources:multi.language, nit%>" ReadOnly="True" SortExpression="nit" />
                                                <asp:BoundField DataField="ciudad" HeaderText="<%$ Resources:multi.language, ciudad%>" SortExpression="ciudad" />
                                                <asp:BoundField DataField="direccion" HeaderText="<%$ Resources:multi.language, direccion%>" SortExpression="direccion" />
                                            </Columns>
                                        </asp:GridView>
                                        <asp:SqlDataSource ID="dsDatosCliente" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="DatosClienteSelProc" SelectCommandType="StoredProcedure">
                                            <SelectParameters>
                                                <asp:Parameter Name="codigoCliente" Type="String" />
                                                <asp:Parameter Name="nuevo" Type="String" />
                                            </SelectParameters>
                                        </asp:SqlDataSource>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="col-sm-7 col-xs-12">
                            <div class="panel panel-default">
                                <div class="panel-body">
                                    <h5 style="margin-top: 0px; margin-bottom: 5px;">
                                        <asp:Label ID="label2" runat="server" Text="<%$ Resources:multi.language, detalle_pedido%>"></asp:Label>
                                    </h5>
                                    <div class="table-responsive">
                                        <div style="overflow: scroll; height: 280px;">
                                            <asp:GridView ID="gvdetalle" runat="server" AutoGenerateColumns="False" CssClass="gvHeader" DataSourceID="dsDetalle" EnableModelValidation="True"
                                                ShowFooter="true" OnRowDataBound="gvdetalle_RowDataBound" Width="100%">
                                                <Columns>
                                                    <asp:BoundField DataField="orden" HeaderText="<%$ Resources:multi.language, orden%>" ReadOnly="True" SortExpression="orden" />
                                                    <asp:BoundField DataField="Codigo" HeaderText="<%$ Resources:multi.language, codigo%>" SortExpression="Codigo" />
                                                    <asp:BoundField DataField="descripcion" HeaderText="<%$ Resources:multi.language, descripcion%>" SortExpression="descripcion" />
                                                    <asp:BoundField DataField="Precio" HeaderText="<%$ Resources:multi.language, precio%>" SortExpression="Precio" ItemStyle-HorizontalAlign="right" />
                                                    <asp:BoundField DataField="Cantidad" HeaderText="<%$ Resources:multi.language, cantidad%>" SortExpression="Cantidad" ItemStyle-HorizontalAlign="right" />
                                                    <asp:BoundField DataField="Valor_Bruto" HeaderText="<%$ Resources:multi.language, valor_bruto%>" ReadOnly="True" SortExpression="Valor_Bruto" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="right" />
                                                    <asp:BoundField DataField="Descuento" HeaderText="<%$ Resources:multi.language, descuento%>" ReadOnly="True" SortExpression="Descuento" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="right" />
                                                    <asp:BoundField DataField="Valor_Neto" HeaderText="<%$ Resources:multi.language, valor_neto%>" ReadOnly="True" SortExpression="Valor_Neto" DataFormatString="{0:C0}" ItemStyle-HorizontalAlign="right" />
                                                </Columns>
                                                <FooterStyle CssClass="gvHeader-footer" />
                                            </asp:GridView>
                                            <asp:SqlDataSource ID="dsDetalle" runat="server" ConnectionString="<%$ ConnectionStrings:Arquitectos %>" SelectCommand="DetallePedidoSelProc" SelectCommandType="StoredProcedure">
                                                <SelectParameters>
                                                    <asp:Parameter Name="documento" Type="String" />
                                                </SelectParameters>
                                            </asp:SqlDataSource>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="col-sm-5 col-xs-12">
                            <div class="panel panel-default">
                                <div class="panel-body">
                                    <h5 style="margin-top: 0px; margin-bottom: 5px;">
                                         <asp:Label ID="label3" runat="server" Text="<%$ Resources:multi.language, ubicacion%>"></asp:Label>
                                    </h5>
                                    <div id="map"></div>
                                </div>
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
            <asp:PostBackTrigger ControlID="btnValorMinimo" />
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
