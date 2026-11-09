function printconsole() {
    //la referencia de la ventana es el objeto window del popup. Lo utilizo para acceder al método close
    console.log('entro a la funcionsss s');
    $('#<%=gvDetalleLista.ClientID %>').Scrollable({
        ScrollHeight: 300,
        IsInUpdatePanel: true
    });
}

function cerrarVentana() {
    //la referencia de la ventana es el objeto window del popup. Lo utilizo para acceder al método close
    ventana_secundaria.close()
}

function Validate() {
    var isValid = false;
    isValid = Page_ClientValidate('rutero');
    if (isValid) {
        isValid = Page_ClientValidate('registro');
    }
    return isValid;
}


//Copy and adapt from 


function gvScroll(gvScroll) {
    var position = 0;
    $(document).ready(function() {
        var grid = document.getElementById(gvScroll);

        $(grid).Scrollable({
            ScrollHeight: 300,
            IsInUpdatePanel: true
        });
    });
}





// funcion para seleccionar todos los checkbox de un gridview 
function selectAllGridView(checkBox) {

    var table = $(checkBox).parents('table');

    if (checkBox.checked == true) {
        $("#" + $(table).attr('id') + " input[type=checkbox]").not(checkBox).prop('checked', true);
    } else {
        $("#" + $(table).attr('id') + " input[type=checkbox]").not(checkBox).prop('checked', false);
    }
}   