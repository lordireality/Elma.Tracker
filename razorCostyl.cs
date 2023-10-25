<input style="display:inline-block; float:right" type="button" onclick="ExportToExcel()" value="Выгрузить в Excel">
<script>
function ExportToExcel(){
	var htmltable= document.getElementById("waterfallPlanner");
	var html = htmltable.outerHTML;
	window.open('data:application/vnd.ms-excel,' + encodeURIComponent(html));
}
const mouseClickEvents = ['mousedown', 'click', 'mouseup'];function simulateMouseClick(element){mouseClickEvents.forEach(mouseEventType =>element.dispatchEvent(new MouseEvent(mouseEventType, {view: window,bubbles: true,cancelable: true,buttons: 1})));}

function SelectPlanToEdit(planitemid){var elems = document.getElementsByTagName("span");var elem=null;for(let i=0; i<elems.length;i++){if(elems[i].innerHTML == 'TRACKER_ITEM_'+planitemid){elem = elems[i].parentElement;}}simulateMouseClick(elem);}
document.getElementsByClassName("ant-alert ant-alert-success ant-alert-no-icon")[0].style = "display:none";
</script>