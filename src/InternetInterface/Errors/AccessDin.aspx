<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AccessDin.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Ошибка авторизации</title>
<link href="../Styles/style.css" type="text/css" rel="stylesheet" />
<link rel="stylesheet" href="../Styles/base.css" type="text/css" media="screen" />
</head>
<body>
<div id="main" style="width: 100%;">
    <div id="container">
        <div id="block-tables" class="block" style="margin: 150px auto;">
           <div class="flash">
               <div class="message error">
                   <p>У вас недостаточно прав для просмотра запрошенной страницы</p></br>
                   <p>обратитесь к администратору системы, если Вам требуется доступ к закрытой функции</p>
                   <a href="../Login/LoginPartner.rails">Перейти на страницу авторизации</a>
               </div>
           </div>
        </div>
    </div>
</div>
</body>
</html>
