<%@ page title="" language="C#" masterpagefile="~/menu.master" autoeventwireup="true" inherits="Informes_ImpresionActas, App_Web_ypghtelr" %>

<%@ Register Assembly="CrystalDecisions.Web, Version=13.0.2000.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" Namespace="CrystalDecisions.Web" TagPrefix="CR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <CR:CrystalReportViewer ID="Crystal" runat="server" AutoDataBind="true" />
</asp:Content>

