
// permite cargar lo eventos del los componentes cuando se usa updatepanels
Sys.WebForms.PageRequestManager.getInstance().add_endRequest(EndRequestHandler);

function EndRequestHandler(sender, args) {
    console.log('prueba updatepanel');
    if (args.get_error() == undefined) {//Condicion si queremos manejar que hubo un error y no ejecutar nada
        iniciarScript();
    }
}


$(document).ready(function () {

    iniciarScript();
});

function iniciarScript() {
    $(".filtrar").before("<div class='row'><div class='col-sm-2'><div id='flip'><i class='fa fa-th-list' style='text-align: center;'>Columnas</i></div><div id='list-checks'></div></div><div class='col-sm-3'><div id='search'><i class='fa fa-search' style='text-align: center;'></i><input type='text' id='texto' /></div></div></div>");
    //$(".registros").before("<label id='contRegistros' text='rrr'></label>");


    var nFilas = $(".registros >tbody tr").length - 1;
    //console.log("filas: " + nFilas);
    ///////////////// 
    //agregar una nueva columna con todo el texto 
    //contenido en las columnas de la grilla 
    // contains de Jquery es CaseSentive, por eso a minúscula
    $(".filtrar  tr:has(td)").each(function () {
        //console.log("filtar");

        var t = $(this).text().toLowerCase();
        $("<td class='indexColumn'></td>")
            .hide().text(t).appendTo(this);
    });

    //Agregar el comportamiento al texto (se selecciona por el ID)
    $("#texto").keyup(function () {
        //console.log("texto");

        var s = $(this).val().toLowerCase().split(" ");
        $(".filtrar tr:hidden").show();
        if (s != "") {
            $.each(s, function () {
                $(".filtrar tr:visible .indexColumn:not(:contains('" + this + "'))").parent().hide();
            });
        }
    });
    ///////////////// 
    //$(".hide-column").before("<div><div id='flip'><i class='fa fa-th-list' style='text-align: center;'> Columnas</i></div> <div id='list-checks'></div></div>");

    // ocultar o mostrar la lista de checks 
    $("#flip").click(function (e) {

        $("#list-checks").slideToggle("slow");

    });

    var columnas = $(".hide-column tr th").length;

    //console.log("columnas");
    //console.log(columnas);
    // se crean los checkbox con los nombres de las columnas
    for (var i = 0; i < columnas; i++) {

        $("#list-checks").append('<div class="check"><input type="checkbox"  checked="true" onChange="ocultar(this,' + i + ')" name="titulos" value="' + i + '"> <span>' + $('.hide-column tr th:eq(' + i + ')').html() + '</span></div>');

    }
}

function ocultar(checbox,pos){

    fila=$('.hide-column tr');

    // mostrar u ocultar las cabezeras de la tabla
	$('.hide-column tr:eq(0)').find('th:eq('+pos+')').toggle();
			

		   //mostramos u ocultamos las celdas de la columna seleccionada
	for(i=1;i<fila.length;i++)
	{
	   	$('.hide-column tr:eq('+i+')').find('td:eq('+pos+')').toggle();     
	}  

}

