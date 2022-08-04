<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebForm2.aspx.cs" Inherits="WebForm2" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>WebForm2</title>
    <style>
        table 
        {            
            table-layout: fixed; 
        }
        /*th {
            cursor: e-resize;
        }*/
        /*
            How do I hide an element when printing a web page?
            https://stackoverflow.com/questions/355313/how-do-i-hide-an-element-when-printing-a-web-page
        */
        @media print 
        {
            .no-print 
            {
                visibility: hidden;
            }
        }
    </style>
    <link href="Content/DataTables/css/jquery.dataTables.css" rel="stylesheet" />
    <script src="Scripts/jquery-3.6.0.js"></script>
    <script>
        /*
           For dynamic sizing by dragging table header. 
        */
        $(document).ready(function () {
            let pressed = false;
            let start = undefined;
            let startX, startWidth;

            $("table th").mousedown(function (e) {
                start = $(this);
                pressed = true;
                startX = e.pageX;
                startWidth = $(this).width();
                $(start).addClass("resizing");
            });

            $(document).mousemove(function (e) {
                if (pressed) {
                    $(start).width(startWidth + (e.pageX - startX));
                }
            });

            $(document).mouseup(function () {
                if (pressed) {
                    $(start).removeClass("resizing");
                    pressed = false;
                }
            });
        });
    </script>

    <script src="Scripts/DataTables/jquery.dataTables.js"></script>
    <script src="Scripts/DataTables/dataTables.colReorder.js"></script>
    <script>
        /*
           For paging and sorting by clicking table header. 
        */
        $(document).ready(function () {
            // var table = $('#GridView1').DataTable({
            //     searching: false,
            //     paging: false,
            //     language: {
            //         sInfo: '顯示第 _START_ 至 _END_ 項結果, 共 _TOTAL_ 項',
            //         sInfoEmpty: '顯示第 0 至 0 項結果, 共 0 項'
            //     }
            // });

            //var table = $('#GridView1').DataTable({colReorder: true});
            var table = $('#GridView1').DataTable();

            $('input.toggle-checkbox').click(function () {
                // Get the column API object
                var column = table.column($(this).attr('data-column'));

                // Toggle the visibility
                column.visible(!column.visible());

                $('#GridView1').width("100%");
            });
        });
    </script>
</head>
<body>
    <form id="WebForm" runat="server">
    <div class="no-print">
        顯示欄位: </<br />
        <span>
            <input class="toggle-checkbox" id="toggle-column-0" type="checkbox" data-column="0" checked />
            <label for="toggle-column-0">PHYSTS</label>
        </span>, 
        <span>
            <input class="toggle-checkbox" id="toggle-column-1" type="checkbox" data-column="1" checked />
            <label for="toggle-column-1">PHYDES</label>
        </span>
        <span>
            <input class="toggle-checkbox" id="toggle-column-2" type="checkbox" data-column="2" checked />
            <label for="toggle-column-1">PHYDESC</label>
        </span>
        <span>
            <input class="toggle-checkbox" id="toggle-column-3" type="checkbox" data-column="3" checked />
            <label for="toggle-column-1">UPDATE_IDENT</label>
        </span>
    </div>

    <div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CellPadding="4" DataKeyNames="PHYSTS" ForeColor="#333333" GridLines="None">
            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
            <Columns>
                <asp:BoundField DataField="PHYSTS" HeaderText="PHYSTS" ReadOnly="True" SortExpression="PHYSTS" />
                <asp:BoundField DataField="PHYDES" HeaderText="PHYDES" SortExpression="PHYDES" />
                <asp:BoundField DataField="PHYDESC" HeaderText="PHYDESC" SortExpression="PHYDESC" />
                <asp:BoundField DataField="UPDATE_IDENT" HeaderText="UPDATE_IDENT" SortExpression="UPDATE_IDENT" />
            </Columns>
            <EditRowStyle BackColor="#999999" />
            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
            <SortedAscendingCellStyle BackColor="#E9E7E2" />
            <SortedAscendingHeaderStyle BackColor="#506C8C" />
            <SortedDescendingCellStyle BackColor="#FFFDF8" />
            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
        </asp:GridView>
    </div>
    </form>
</body>
</html>