<%@ page language="VB" autoeventwireup="false" inherits="Index, App_Web_qvliwonv" title="CELUWEB" %>

<!DOCTYPE html >
<html lang="es">
<head id="Head1" runat="server">
    <title>Celuweb S.A.S</title>
    <meta charset="utf-8">
    <!-- Mobile viewport optimized: h5bp.com/viewport -->
    <meta name="viewport" content="width=device-width">

    <!-- CSS -->
    <link rel="stylesheet" type="text/css" href="Styles/bootstrap/dist/css/bootstrap.css" />
    <link href="Styles/css/styles.css" rel="Stylesheet" rev="stylesheet" />

    <!--Scripts-->
    <script src="http://code.jquery.com/jquery-1.9.1.min.js" type="text/javascript"></script>
    <%--<script src="Styles/js/jquery-1.4.1.min.js" type="text/javascript"></script>--%>
    <%--<script src="Styles/js/ScrollableGridPlugin_ASP.NetAJAX_3.0.js" type="text/javascript"></script>--%>
    <script src="Styles/js/Funciones.js" type="text/javascript"></script>
    <%--<script src="Styles/js/bootstrap.min.js" type="text/javascript"></script>--%>
    <script src="Styles/bootstrap/js/bootstrap.js" type="text/javascript"></script>

</head>
<body>
    <div style="position: fixed; top: 0; width: 100%; z-index: -1; height: 100%;">
        <img src="Styles/images/fondo-Arquitectos.png" style="width: 100%; height: 100%;" />
    </div>
    <form id="form1" runat="server">
        <div style="margin: auto; max-width: 500px; margin-top: 50px;">
            <div style="text-align: center; margin-bottom: 30px;">
                <img src="Styles/images/logoini-Arquitectos.png" />
            </div>
            <div>
                <div style="background-image: url(Styles/images/login-Arquitectos.png); background-repeat: no-repeat; height: 275px; width: 500px;">
                    <input id="txtUsuario" runat="server" type="text" name="username" required="required" placeholder="usuario"
                        style="position: absolute; margin-top: 56px; width: 220px; margin-left: 175px; height: 35px; border: none; background-color: rgba(0, 0, 0, 0); color: white; font-size: 18px;">
                    <input id="txtClave" runat="server" type="password" name="password" required="required" placeholder="password"
                        style="position: absolute; margin-top: 108px; width: 220px; margin-left: 175px; height: 35px; border: none; background-color: rgba(0, 0, 0, 0); color: white; font-size: 18px;">
                    <div>
                        <asp:Button ID="bIngresar" runat="server" CssClass="botonLogin" />
                    </div>
                </div>
            </div>
        </div>
    </form>
    <footer style="text-align: center; margin-top: 50px;">
        <img src="Styles/images/logo-celuwebsas-login.png" />
    </footer>
</body>
</html>
