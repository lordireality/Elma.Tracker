using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EleWise.ELMA.Core;
using EleWise.ELMA.Core.Controllers;
using EleWise.ELMA.Core.Services;
using EleWise.ELMA.Model.Common;
using EleWise.ELMA.Model.Validation;
using EleWise.ELMA.Model.ViewModel;
using EleWise.ELMA.Model.Views;
using EleWise.ELMA.DataClasses;
using EleWise.ELMA.Core.Services;
using EleWise.ELMA.Services;
using System.Runtime;

using System.Text;

using EleWise.ELMA.Model.Entities;

namespace EleWise.ELMA.UI.Components
{
    /// <summary>
    /// Контроллер компонента "Планирование"
    /// </summary>
    partial class Planner
    {
        partial class ComponentController
        {
            public static string t_style = "<style>.planner-label{margin: 1px; border: 1px solid black;border-radius: 15px;text-align: center;display: inline-block;padding: 2px;background-color: white;} .planner-t{font-size: 11px; width: 100%;border-collapse: collapse;}.planner-h{width: 8.3%; border-bottom:1px solid black; border-left: 1px solid #d0d0d0;border-right: 1px solid #d0d0d0;text-align: center;}.planner-em{width: 1%; text-align: left;border-left: 1px solid #d0d0d0;border-right: 1px solid #d0d0d0; border-bottom: 1px solid black; }.planner-it{text-align: left; border-bottom: 1px solid black;}.plan-tooltip .plan-tooltiptext { visibility: hidden; background-color: black; color: #fff; text-align: left; border-radius: 6px; padding: 15px; position: absolute; z-index: 1;}.plan-tooltip .plan-tooltiptext::after {content: \"\";position: absolute;bottom: 100%;left: 50%;margin-left: -5px;border-width: 5px;border-style: solid;border-color: transparent transparent black transparent;}.plan-tooltip:hover .plan-tooltiptext {visibility: visible;}.plan-tooltip { text-decoration: underline dotted black;}</style>";
            public static string t_header = "<tr><td class=\"planner-h\" colspan=\"4\">Январь</td><td class=\"planner-h\" colspan=\"4\">Февраль</td><td class=\"planner-h\" colspan=\"4\">Март</td><td class=\"planner-h\" colspan=\"4\">Апрель</td><td class=\"planner-h\" colspan=\"4\">Май</td><td class=\"planner-h\" colspan=\"4\">Июнь</td><td class=\"planner-h\" colspan=\"4\">Июль</td><td class=\"planner-h\" colspan=\"4\">Август</td><td class=\"planner-h\" colspan=\"4\">Сентябрь</td><td class=\"planner-h\" colspan=\"4\">Октябрь</td><td class=\"planner-h\" colspan=\"4\">Ноябрь</td><td class=\"planner-h\" colspan=\"4\">Декабрь</td></tr>";
            public static string t_close = "</table>";
            public static string t_open = "<table id=\"waterfallPlanner\" class=\"planner-t\">";
            

            private readonly INotificationService notificationService;
			private readonly ILoaderService loaderService;
            private readonly IRedirectService redirectService;

            public ComponentController(INotificationService notificationService, ILoaderService loaderService, IRedirectService redirectService){
                this.notificationService = notificationService;
				this.loaderService = loaderService;
                this.redirectService = redirectService;
            }

            public int startIndexCalc(DateTime startDateLocal){
                if(startDateLocal.Year == Context.CurrentYear){
                    var daysIn = DateTime.DaysInMonth(startDateLocal.Year, startDateLocal.Month);
                    double toFloor = startDateLocal.Day/(daysIn/4)+1;
                    var week = (int)Math.Floor(toFloor);
                    return ((startDateLocal.Month-1) * 4) + week;
                } else {
                    return 0;
                }
            }
            public int endIndexCalc(DateTime endDateLocal){
                if(endDateLocal.Year == Context.CurrentYear){
                    var daysIn = DateTime.DaysInMonth(endDateLocal.Year, endDateLocal.Month);
                    double toFloor = endDateLocal.Day/(daysIn/4)+1;
                    var week = (int)Math.Floor(toFloor);
                    return ((endDateLocal.Month-1) * 4) + week;
                } else {
                    return 48;
                }
            }
            public string leftColumnBuilder(int startIndex){
                string columnHtml = "";
                //Рендерим все пустые колонки слева
                for(var i =1; i< startIndex; i++){
                    columnHtml+="<td class=\"planner-em\">&nbsp</td>";
                }
                return columnHtml;
            }
            public string rightColumnBuilder(int endIndex){
                string columnHtml = "";
                //Рендерим все пустые колонки слева
                for(var i =endIndex; i< 49; i++){
                    columnHtml+="<td class=\"planner-em\">&nbsp</td>";
                }
                return columnHtml;
            }


            public async System.Threading.Tasks.Task BuildPlan(){
                loaderService.Show("1", "Загрузка");
                Context.PlanItems.Clear();
                string t_html = "";
                t_html += t_style;
                t_html += t_open;
                t_html += t_header;
                Context.EditCats.Clear();
                if(Context.Plan != null){
                    
                    foreach(var item in Context.Plan.TrackerElemCategory){
                        Context.EditCats.Add(new CategoriesDTO(){Name = item.Name, NamePretty = new System.Web.HtmlString(string.Format("<p style=\"margin: 1px; border: 1px solid black;border-radius: 15px;text-align: center;display: inline-block;padding: 2px;background-color: {0};\">{1}</p>", item.ColorCode, item.Name))});
                    }
                    
                    var accessResult = await Context.CheckRightsAction(Context.Plan.Id);
                    Context.CanViewCurrentPlan = accessResult.View;
                    if(accessResult.View == true){
                        Context.IsCurrentUserManager = accessResult.Edit;
                        var dto = new Planner.SelPlanDto();
                        dto.Year = Context.CurrentYear;
                        dto.PlanId = Context.Plan.Id;
                        dto.State = Context.FiljtrPoSostoyaniyu != null ? Context.FiljtrPoSostoyaniyu.Value : "Все";
                        var result = await Context.SelPlanItemsAction(dto);
                                                
                        var resOrdered = result.OrderBy(c=>c.StartDate);
                        foreach(var item in resOrdered){
                            Context.PlanItems.Add(item);
                            int startIndex = startIndexCalc(item.StartDate);
                            int endIndex = endIndexCalc(item.EndDate);
                            
                            t_html += "<tr>";
                            t_html += leftColumnBuilder(startIndex);
                            if(startIndex != endIndex){
                                string content = "<b>";
                                if(Context.IsCurrentUserManager){
                                    content += string.Format("<button type=\"button\" onclick=\"SelectPlanToEdit({0})\" tooltiptext=\"Редактировать\" class=\"t-button t-button-icon circle t-button-nofill t-button-noborder\"><i class=\"svg-element t-button-image\" data-source=\"/Content/IconPack/edit.svg\"><!--?xml version=\"1.0\" encoding=\"utf-8\"?--><svg x=\"0\" y=\"0\" width=\"1024\" height=\"1024\" overflow=\"hidden\" viewBox=\"0, 0, 1024, 1024\" preserveAspectRatio=\"xMidYMid\" font-size=\"0\" color=\"#B1B1B1\" xml:space=\"default\" style=\"color:currentColor;fill:none;\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" version=\"1.1\"><path d=\"M795.859 149.086 L874.958 228.286 C888.434 241.79 896.002 260.1 895.998 279.189 C895.994 298.279 888.419 316.586 874.938 330.085 L856.382 348.664 L675.612 167.666 L694.168 149.086 C707.653 135.585 725.943 128 745.013 128 C764.084 128 782.374 135.585 795.859 149.086 z M128 746.792 C128.023 727.69 135.621 709.379 149.123 695.883 L627.425 217.008 L806.138 395.946 L327.866 874.851 C314.387 888.37 296.099 895.978 277.021 896.001 L200.028 896.001 C180.933 895.977 162.626 888.371 149.123 874.851 C135.62 861.332 128.024 843.002 128 823.882 L128 746.792 z M199.909 824.002 L277.021 824.002 L704.417 396.006 L627.365 318.797 L199.909 746.792 L199.909 824.002 z\" clip-rule=\"evenodd\" fill-rule=\"evenodd\" xml:space=\"default\" style=\"fill:currentColor;\"></path></svg></i></button>",item.RecordId.ToString());
                                }
                                
                                content += string.Format("<p class=\"planner-label\">{0} / {1}</p>",item.LabelName, item.Status);
                                content += string.Format("{0},<br> Плановый срок исполнения:</b><br>{1} по {2}", item.Name, item.StartDate.ToString("dd.MM.yyyy"), item.EndDate.ToString("dd.MM.yyyy"));
                                if(item.Executors.Count != 0 || item.RelProcess.Count != 0 || item.RelTask.Count != 0 || item.RelDoc.Count != 0 || item.RelProject.Count != 0){
                                    content+="<div class=\"plan-tooltip\">Связанные объекты системы";
                                    content+="<div class=\"plan-tooltiptext\">";
                                    if(item.Executors.Count != 0){
                                        content += "<b>Исполнители:</b>";
                                        foreach(var exec in item.Executors){
                                            content += string.Format("<a href=\"javascript:showUserInfo('{0}');\">{1} {2}.{3}.</a> ", exec.Id.ToString(), exec.LastName, exec.FirstName.Substring(0, 1), exec.MiddleName.Substring(0, 1));
                                        }
                                    }
                                    
                                    if(item.RelProcess.Count != 0){
                                        content += "<br><b>Процессы:</b>";
                                        foreach(var proc in item.RelProcess){
                                            
                                            content += string.Format("<br><a href=\"/Processes/WorkflowInstance/Info/{0}\">{1}</a>", proc.Id.ToString(), proc.Name);
                                        }
                                    }
                                    if(item.RelTask.Count != 0){
                                        content += "<br><b>Задачи:</b>";
                                        foreach(var task in item.RelTask){
                                            content+=string.Format("<br><p>{0}</p>",task.Subject);
                            

                                        }
                                    }
                                    if(item.RelDoc.Count != 0){
                                        content += "<br><b>Документы:</b>";
                                        foreach(var doc in item.RelDoc){
                                            
                                            content += string.Format("<br><a href=\"/Documents/Document/View/{0}\">{1}</a>", doc.Id.ToString(), doc.Name);
                                        }
                                    }
                                    if(item.RelProject.Count != 0){
                                        content += "<br><b>Проекты:</b>";
                                        foreach(var proj in item.RelProject){
                                            //
                                            content += string.Format("<br><a href=\"/Projects/Project/AllInfo/{0}\">{1}</a>", proj.Id.ToString(), proj.Name);

                                        }
                                    }
                                    content += "</div></div>";
                                }


                                

                                int diff = endIndex-startIndex;
                                t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),content);
                            }
                            t_html += rightColumnBuilder(endIndex);
                            t_html += "</tr>";
                            //Отображение факта
                            if(item.EndFact != null && item.StartFact != null){
                                int startIndexFact = startIndexCalc(item.StartFact.Value);
                                int endIndexFact = endIndexCalc(item.EndFact.Value);
                                t_html += "<tr>";
                                t_html += leftColumnBuilder(startIndexFact);
                                if(startIndexFact != endIndexFact){
                                    int diff = endIndexFact-startIndexFact;
                                    string factContent = string.Format("<b>Фактический срок исполнения:</b><br>{0} по {1}", item.StartFact.Value.ToString("dd.MM.yyyy"), item.EndFact.Value.ToString("dd.MM.yyyy"));
                                    t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}; filter: brightness(80%);\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),factContent);
                                }
                                t_html += rightColumnBuilder(endIndexFact);
                                t_html += "</tr>";
                            }


                        }
                        t_html += t_close;
                        Context.HtmlDebug = t_html;
                        Context.BuildedHTMLPlan = new System.Web.HtmlString(t_html);
                    }
                    else {
                        Context.IsCurrentUserManager = false;

                        Context.BuildedHTMLPlan = new System.Web.HtmlString("<div class=\"warning-panel\"><div class=\"warning-panel-header\"><span>Недостаточно прав доступа для просмотра плана!</span></div></div>");
                    }                
                
                

                } else {
                    Context.BuildedHTMLPlan = new System.Web.HtmlString(" ");
                }
                loaderService.Hide("1");
            } 


            /// <summary>
            /// Сценарий при изменении значения свойства "План"
            /// </summary>
            public async System.Threading.Tasks.Task PlanOnChange(IChangeEvent<EleWise.ELMA.ConfigurationModel.PlanTracker> e)
            {
                await BuildPlan();

            }


            /// <summary>
            /// Сценарий вычисления значения свойства "Hide" элемента отображения Property8
            /// </summary>
            public bool HideIfPlanNotSelected()
            {
                if(Context.Plan != null){
                    return !Context.CanViewCurrentPlan;
                } else {
                    return true;
                }
            }

            /// <summary>
            /// Сценарий при изменении значения свойства "Фильтр по состоянию"
            /// </summary>
            public async System.Threading.Tasks.Task StatusOnChange(IChangeEvent<DropDownItem> e)
            {
                await BuildPlan();
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button3
            /// </summary>
            public async System.Threading.Tasks.Task NextYear()
            {
                Context.CurrentYear +=1;
                await BuildPlan();
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button1
            /// </summary>
            public async System.Threading.Tasks.Task PreviousYear()
            {
                Context.CurrentYear -=1;
                await BuildPlan();
            }

            /// <summary>
            /// Сценарий вычисления значения свойства "Hide" элемента отображения Panel2
            /// </summary>
            public bool HideIfNotManager()
            {
                return !Context.IsCurrentUserManager;
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button4
            /// </summary>
            public void DebugBtnClick()
            {
                Context.BuildedHTMLPlan = new System.Web.HtmlString("<div class=\"warning-panel\"><div class=\"warning-panel-header\"><span>Недостаточно прав доступа для просмотра плана!</span></div></div>");
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button4
            /// </summary>
            public void SelectItem(PlanRowDTO item0)
            {
                Context.IsEditingState = true;
                loaderService.Show("2", "Загрузка");

                //очищаем
                Context.EditEndDate = null;
                Context.EditStartDate = null;
                Context.EditStartDateFact = null;
                Context.EditEndDateFact = null;
                Context.EditName = "";
                Context.EditExecutors.Clear();
                Context.SelectedCatsEdit.Clear();
                Context.EditStatus = null;
                Context.EditRelatedWfi.Clear();
                Context.EditRelatedTasks.Clear();
                Context.EditRelatedDocs.Clear();
                Context.EditRelProj.Clear();

                //заполняем

                Context.EditName = item0.Name;
                Context.EditStartDate = item0.StartDate;
                Context.EditEndDate = item0.EndDate;
                Context.EditPlanItemId = item0.RecordId;
                Context.EditEndDateFact = item0.EndFact;
                Context.EditStartDateFact = item0.StartFact;
                Context.SelectedCatsEdit.Clear();
                Context.SelectedCatsEdit.Add(Context.EditCats.Where(c=>c.Name == item0.LabelName).FirstOrDefault());
                Context.EditStatus = new DropDownItem(item0.Status);
                foreach(var item in item0.Executors){
                    Context.EditExecutors.Add(item);
                }

                foreach(var item in item0.RelProcess){
                    Context.EditRelatedWfi.Add(item);
                }
                foreach(var item in item0.RelTask){
                    Context.EditRelatedTasks.Add(item);
                }
                foreach(var item in item0.RelDoc){
                    Context.EditRelatedDocs.Add(item);
                }
                foreach(var item in item0.RelProject){
                    Context.EditRelProj.Add(item);
                }

                loaderService.Hide("2");
                
            }

            /// <summary>
            /// Сценарий вычисления значения свойства "Text" элемента отображения Button4
            /// </summary>
            public string PlanItemButtonFormName(PlanRowDTO item0)
            {
                return "TRACKER_ITEM_" + item0.RecordId.ToString();
            }

            /// <summary>
            /// Сценарий вычисления значения свойства "Hide" элемента отображения Panel2
            /// </summary>
            public bool NotEditState()
            {
                return !Context.IsEditingState;
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button5
            /// </summary>
            public void CancelEdit()
            {
                Context.IsEditingState = false;
                Context.EditEndDate = null;
                Context.EditStartDate = null;
                Context.EditStartDateFact = null;
                Context.EditEndDateFact = null;
                Context.EditName = "";
                Context.EditExecutors.Clear();
                Context.SelectedCatsEdit.Clear();
                Context.EditStatus = null;
                Context.EditRelatedWfi.Clear();
                Context.EditRelatedTasks.Clear();
                Context.EditRelatedDocs.Clear();
                Context.EditRelProj.Clear();
                if(Context.IsPreviewState){
                    Context.IsPreviewState = false;
                }
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button6
            /// </summary>
            public void BuildPreviewPlan()
            {
                if(Context.IsPreviewState == true){
                    Context.IsPreviewState = false;
                    return;
                }
                Context.IsPreviewState = true;
                loaderService.Show("3", "Загрузка предпросмотра плана");
                string t_html = "<h1 style=\"text-align:center;\">Вы находитесь в режиме предпросмотра плана! В режиме предпросмотра, связанные объекты отображены не будут.</h1>";
                
                t_html += t_open;
                t_html += t_header;
                List<PlanRowDTO> previews = new List<PlanRowDTO>();
                foreach(var item in Context.PlanItems){
                    if(item.RecordId != Context.EditPlanItemId){
                        previews.Add(item);
                    } else {
                        var newRow = new PlanRowDTO();
                        newRow.Name = "[Предпросмотр]" + Context.EditName;
                        newRow.EndDate = Context.EditEndDate.Value;
                        newRow.EndFact = Context.EditEndDateFact;
                        newRow.StartDate = Context.EditStartDate.Value;
                        newRow.StartFact = Context.EditStartDateFact;
                        newRow.Status = Context.EditStatus.Value;
                        newRow.LabelName = Context.SelectedCatsEdit.FirstOrDefault().Name;
                        newRow.LabelColorCode = "#ff7e7e";
                        previews.Add(newRow);
                    }
                }
                foreach(var item in previews){
                    
                    int startIndex = startIndexCalc(item.StartDate);
                    int endIndex = endIndexCalc(item.EndDate);
                    t_html += "<tr>";
                    t_html += leftColumnBuilder(startIndex);
                    if(startIndex != endIndex){
                        string content = "<b>";       
                        content += string.Format("<p class=\"planner-label\">{0} / {1}</p>",item.LabelName, item.Status);
                        content += string.Format("{0},<br> Плановый срок исполнения:</b><br>{1} по {2}", item.Name, item.StartDate.ToString("dd.MM.yyyy"), item.EndDate.ToString("dd.MM.yyyy"));
                        int diff = endIndex-startIndex;
                        t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),content);
                    }
                    t_html += rightColumnBuilder(endIndex);
                    t_html += "</tr>";
                    //Отображение факта
                    if(item.EndFact != null && item.StartFact != null){
                        int startIndexFact = startIndexCalc(item.StartFact.Value);
                        int endIndexFact = endIndexCalc(item.EndFact.Value);
                        t_html += "<tr>";
                        t_html += leftColumnBuilder(startIndexFact);
                        if(startIndexFact != endIndexFact){
                            int diff = endIndexFact-startIndexFact;
                            string factContent = string.Format("<b>Фактический срок исполнения:</b><br>{0} по {1}", item.StartFact.Value.ToString("dd.MM.yyyy"), item.EndFact.Value.ToString("dd.MM.yyyy"));
                            t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}; filter: brightness(80%);\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),factContent);
                        }
                        t_html += rightColumnBuilder(endIndexFact);
                        t_html += "</tr>";
                    }


                }
                t_html += t_close;
                Context.HtmlDebug = t_html;
                Context.PreviewHTMLPlan = new System.Web.HtmlString(t_html);
                loaderService.Hide("3");
            }

            /// <summary>
            /// Сценарий вычисления значения свойства "Hide" элемента отображения RowLayout1
            /// </summary>
            public bool HideInPreviewState()
            {
                return Context.IsPreviewState;
            }

            /// <summary>
            /// Сценарий вычисления значения свойства "Hide" элемента отображения PropertyValue2
            /// </summary>
            public bool HideIfNotPreview()
            {
                return !Context.IsPreviewState;
            }

            /// <summary>
            /// Сценарий действия "Метод при выборе элемента"
            /// </summary>
            public void SelectCategory(bool arg, CategoriesDTO item0)
            {
                Context.SelectedCatsEdit.Clear();
                if(true){
                    Context.SelectedCatsEdit.Add(item0);
                }
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button2
            /// </summary>
            public async System.Threading.Tasks.Task SaveEditBtnOnClick()
            {
                loaderService.Show("4", "Внесение изменений");
                var editItem = new PlanRowDTO();

                //название
                if(string.IsNullOrEmpty(Context.EditName)){
                    notificationService.Error("Ошибка валидации!", "Поле Наименование не заполнено!");
                    loaderService.Hide("4");
                    return;
                }
                editItem.RecordId = Context.EditPlanItemId;
                editItem.Name = Context.EditName;
                if(Context.EditStartDate == null){
                    notificationService.Error("Ошибка валидации!", "Поле Дата начала не заполнено!");
                    loaderService.Hide("4");
                    return;
                }
                if(Context.EditEndDate == null){
                    notificationService.Error("Ошибка валидации!", "Поле Дата завершения не заполнено!");
                    loaderService.Hide("4");
                    return;
                }
                if(Context.EditStartDate > Context.EditEndDate){
                    notificationService.Error("Ошибка валидации!", "Поле Дата начала не может быть позднее чем Дата завершения!");
                    loaderService.Hide("4");
                    return;
                }
                editItem.EndDate = Context.EditEndDate.Value;
                editItem.StartDate = Context.EditStartDate.Value;
                if((Context.EditEndDateFact != null && Context.EditStartDateFact == null) || (Context.EditEndDateFact == null && Context.EditStartDateFact != null) ){
                    notificationService.Error("Ошибка валидации!", "Заполните фактический промежуток полностью!");
                    loaderService.Hide("4");
                    return;
                }
                if((Context.EditEndDateFact != null) && (Context.EditStartDateFact != null)){
                    editItem.EndFact = Context.EditEndDateFact;
                    editItem.StartFact = Context.EditStartDateFact;
                }


                if(Context.SelectedCatsEdit.Count == 0){
                    notificationService.Error("Ошибка валидации!", "Поле Категория не заполнено!");
                    loaderService.Hide("4");
                    return;
                }
                editItem.LabelName = Context.SelectedCatsEdit.FirstOrDefault().Name;
                if(Context.EditStatus == null){
                    notificationService.Error("Ошибка валидации!", "Поле Статус не заполнено!");
                    loaderService.Hide("4");
                    return;
                }
                editItem.Status = Context.EditStatus.Value;
                foreach(var item in Context.EditExecutors){
                    editItem.Executors.Add(item);
                }
                foreach(var item in Context.EditRelatedWfi){
                    editItem.RelProcess.Add(item);
                }
                foreach(var item in Context.EditRelatedTasks){
                    editItem.RelTask.Add(item);
                }
                foreach(var item in Context.EditRelatedDocs){
                    editItem.RelDoc.Add(item);
                }
                foreach(var item in Context.EditRelProj){
                    editItem.RelProject.Add(item);
                }

                var result = await Context.EditSaveAction(editItem);
                if(result == true){
                    redirectService.Reload();
                } else {
                    notificationService.Error("Произошла ошибка при сохранении!","Произошла ошибка при сохранении!",false);
                    loaderService.Hide("4");
                }
                
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button12
            /// </summary>
            public async System.Threading.Tasks.Task RemovePlanItem()
            {
                var result = await Context.RemoveItemAction(Context.EditPlanItemId);
                if(result == true){
                    redirectService.Reload();
                }
            }

            /// <summary>
            /// Сценарий действия "Метод при выборе элемента"
            /// </summary>
            public void SelectCategoryCreate(bool arg, CategoriesDTO item0)
            {
                Context.SelectedCatsCreate.Clear();
                if(true){
                    Context.SelectedCatsCreate.Add(item0);
                }
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button8
            /// </summary>
            public void AddToCreateTable()
            {
                loaderService.Show("5", "Добавление в таблицу");
                var dto = new CreateTrackerItem();

                if(string.IsNullOrEmpty(Context.CreateName)){
                    notificationService.Error("Ошибка валидации!", "Поле Наименование не заполнено!");
                    loaderService.Hide("5");
                    return;
                }
                
                dto.Name = Context.CreateName;
                if(Context.CreateStartDate == null){
                    notificationService.Error("Ошибка валидации!", "Поле Дата начала не заполнено!");
                    loaderService.Hide("5");
                    return;
                }
                if(Context.CreateEndDate == null){
                    notificationService.Error("Ошибка валидации!", "Поле Дата завершения не заполнено!");
                    loaderService.Hide("5");
                    return;
                }
                if(Context.CreateStartDate > Context.CreateEndDate){
                    notificationService.Error("Ошибка валидации!", "Поле Дата начала не может быть позднее чем Дата завершения!");
                    loaderService.Hide("5");
                    return;
                }
                dto.EndDate = Context.CreateEndDate.Value;
                dto.StartDate = Context.CreateStartDate.Value;
                if((Context.CreateEndDateFact != null && Context.CreateStartDateFact == null) || (Context.CreateEndDateFact == null && Context.CreateStartDateFact != null) ){
                    notificationService.Error("Ошибка валидации!", "Заполните фактический промежуток полностью!");
                    loaderService.Hide("5");
                    return;
                }
                if((Context.CreateEndDateFact != null) && (Context.CreateStartDateFact != null)){
                    dto.EndDateFact = Context.CreateEndDateFact;
                    dto.StartDateFact = Context.CreateStartDateFact;
                }


                if(Context.SelectedCatsCreate.Count == 0){
                    notificationService.Error("Ошибка валидации!", "Поле Категория не заполнено!");
                    loaderService.Hide("5");
                    return;
                }
                dto.Label = Context.SelectedCatsCreate.FirstOrDefault().Name;
                if(Context.CreateStatus == null){
                    notificationService.Error("Ошибка валидации!", "Поле Статус не заполнено!");
                    loaderService.Hide("5");
                    return;
                }
                dto.State = new DropDownItem(Context.CreateStatus.Value);
                dto.PlanId = Context.Plan.Id;
                foreach(var item in Context.CreateExecutors){
                    dto.Executors.Add(item);
                }
                foreach(var item in Context.CreateRelatedWfi){
                    dto.RelatedProc.Add(item);
                }
                foreach(var item in Context.CreateRelatedTasks){
                    dto.RelTasks.Add(item);
                }
                foreach(var item in Context.CreateRelatedDocuments){
                    dto.RelDocs.Add(item);
                }
                foreach(var item in Context.CreateRelatedProjects){
                    dto.RelProjects.Add(item);
                }
                Context.ItemsToCreate.Add(dto);

                //очистка
                Context.CreateName = "";
                Context.CreateStartDate = null;
                Context.CreateStartDateFact = null;
                Context.CreateEndDate = null;
                Context.CreateEndDateFact = null;
                Context.CreateStatus = null;
                Context.SelectedCatsCreate.Clear();
                Context.CreateExecutors.Clear();
                Context.CreateRelatedWfi.Clear();
                Context.CreateRelatedTasks.Clear();
                Context.CreateRelatedDocuments.Clear();
                Context.CreateRelatedProjects.Clear();
                loaderService.Hide("5");
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button9
            /// </summary>
            public void RemoveSelectedToCreate()
            {
                loaderService.Show("6", "Удаление");
                //мега костыль, что бы не словить эксепшен что коллекция была изменена
                var selectedRowsReflection = new List<CreateTrackerItem>();
                foreach(var item in Context.SelectedRowsToCreate){
                    selectedRowsReflection.Add(item);
                }
                foreach(var item in selectedRowsReflection){
                    Context.ItemsToCreate.Remove(item);
                }
                loaderService.Hide("6");
                
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button10
            /// </summary>
            public void EditFirstSelectedToCreate()
            {
                loaderService.Show("7", "Пожалуйста подождите");
                var rowToCreate = Context.ItemsToCreate.FirstOrDefault();
                if(rowToCreate != null){
                    //очистка
                    Context.CreateName = "";
                    Context.CreateStartDate = null;
                    Context.CreateStartDateFact = null;
                    Context.CreateEndDate = null;
                    Context.CreateEndDateFact = null;
                    Context.CreateStatus = null;
                    Context.SelectedCatsCreate.Clear();
                    Context.CreateExecutors.Clear();
                    Context.CreateRelatedWfi.Clear();
                    Context.CreateRelatedTasks.Clear();
                    Context.CreateRelatedDocuments.Clear();
                    Context.CreateRelatedProjects.Clear();
                    //заполнение
                    Context.CreateName = rowToCreate.Name;
                    Context.CreateStartDate = rowToCreate.StartDate;
                    Context.CreateStartDateFact = rowToCreate.StartDateFact;
                    Context.CreateEndDate = rowToCreate.EndDate;
                    Context.CreateEndDateFact = rowToCreate.EndDateFact;
                    Context.CreateStatus = new DropDownItem(rowToCreate.State.Value);
                    Context.SelectedCatsCreate.Add(Context.EditCats.Where(c=>c.Name == rowToCreate.Label).FirstOrDefault());
                    foreach(var item in rowToCreate.Executors){
                        Context.CreateExecutors.Add(item);
                    }
                    foreach(var item in rowToCreate.RelatedProc){
                        Context.CreateRelatedWfi.Add(item);
                    }
                    foreach(var item in rowToCreate.RelTasks){
                        Context.CreateRelatedTasks.Add(item);
                    }
                    foreach(var item in rowToCreate.RelDocs){
                        Context.CreateRelatedDocuments.Add(item);
                    }
                    foreach(var item in rowToCreate.RelProjects){
                        Context.CreateRelatedProjects.Add(item);
                    }
                    Context.ItemsToCreate.Remove(rowToCreate);
                    loaderService.Hide("7");
                } else {
                    notificationService.Error("Ошибка при редактировании!", "Элемент плана не выбран", true);
                    loaderService.Hide("7");
                }
                
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button11
            /// </summary>
            public void BuildPreviewCreate()
            {
                
                if(Context.IsPreviewState == true){
                    Context.IsPreviewState = false;
                    return;
                }
                Context.IsPreviewState = true;
                Context.IsEditingState = false;
                //костыль на очистку EDIT если он открыт
                Context.EditEndDate = null;
                Context.EditStartDate = null;
                Context.EditStartDateFact = null;
                Context.EditEndDateFact = null;
                Context.EditName = "";
                Context.EditExecutors.Clear();
                Context.SelectedCatsEdit.Clear();
                Context.EditStatus = null;
                Context.EditRelatedWfi.Clear();
                Context.EditRelatedTasks.Clear();
                Context.EditRelatedDocs.Clear();
                Context.EditRelProj.Clear();
                loaderService.Show("3", "Загрузка предпросмотра плана");
                string t_html = "<h1 style=\"text-align:center;\">Вы находитесь в режиме предпросмотра плана! В режиме предпросмотра, связанные объекты отображены не будут.</h1>";
                
                t_html += t_open;
                t_html += t_header;
                List<PlanRowDTO> previews = new List<PlanRowDTO>();
                foreach(var item in Context.PlanItems){
                        previews.Add(item);                    
                }
                foreach(var item in Context.ItemsToCreate){
                    var newRow = new PlanRowDTO();
                    newRow.Name = "[Предпросмотр]" + item.Name;
                    newRow.EndDate = item.EndDate;
                    newRow.EndFact = item.EndDateFact;
                    newRow.StartDate = item.StartDate;
                    newRow.StartFact = item.StartDateFact;
                    newRow.Status = item.State.Value;
                    newRow.LabelName = item.Label;
                    newRow.LabelColorCode = "#ff7e7e";
                    previews.Add(newRow);
                }
                
                foreach(var item in previews){
                    
                    int startIndex = startIndexCalc(item.StartDate);
                    int endIndex = endIndexCalc(item.EndDate);
                    t_html += "<tr>";
                    t_html += leftColumnBuilder(startIndex);
                    if(startIndex != endIndex){
                        string content = "<b>";       
                        content += string.Format("<p class=\"planner-label\">{0} / {1}</p>",item.LabelName, item.Status);
                        content += string.Format("{0},<br> Плановый срок исполнения:</b><br>{1} по {2}", item.Name, item.StartDate.ToString("dd.MM.yyyy"), item.EndDate.ToString("dd.MM.yyyy"));
                        int diff = endIndex-startIndex;
                        t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),content);
                    }
                    t_html += rightColumnBuilder(endIndex);
                    t_html += "</tr>";
                    //Отображение факта
                    if(item.EndFact != null && item.StartFact != null){
                        int startIndexFact = startIndexCalc(item.StartFact.Value);
                        int endIndexFact = endIndexCalc(item.EndFact.Value);
                        t_html += "<tr>";
                        t_html += leftColumnBuilder(startIndexFact);
                        if(startIndexFact != endIndexFact){
                            int diff = endIndexFact-startIndexFact;
                            string factContent = string.Format("<b>Фактический срок исполнения:</b><br>{0} по {1}", item.StartFact.Value.ToString("dd.MM.yyyy"), item.EndFact.Value.ToString("dd.MM.yyyy"));
                            t_html+=string.Format("<td class=\"planner-it\" style=\"background-color:{0}; filter: brightness(80%);\" colspan=\"{1}\">{2}</td>",item.LabelColorCode,diff.ToString(),factContent);
                        }
                        t_html += rightColumnBuilder(endIndexFact);
                        t_html += "</tr>";
                    }


                }
                t_html += t_close;
                Context.HtmlDebug = t_html;
                Context.PreviewHTMLPlan = new System.Web.HtmlString(t_html);
                loaderService.Hide("3");
            }

            /// <summary>
            /// Скрипт при нажатии на кнопку Button12
            /// </summary>
            public async System.Threading.Tasks.Task SaveCreateToPlan()
            {
                loaderService.Show("8", "Сохранение");
                if(Context.ItemsToCreate.Count != 0){
                    var result = await Context.CreateSaveAction(Context.ItemsToCreate.ToArray());
                    if(!result){
                        notificationService.Error("Произошла ошибка при сохранении плана!", "Произошла ошибка при сохранении плана!", false);
                        loaderService.Hide("8");
                    } else {
                        loaderService.Hide("8");
                        redirectService.Reload();
                    }
                } else {
                    notificationService.Error("Произошла ошибка при сохранении плана!", "Нет записей для сохранения!", false);
                }
                
            }

            /// <summary>
            /// Сценарий действия "Метод при выборе элемента"
            /// </summary>
            public void AddCreateRowToSelected(bool arg, CreateTrackerItem item0)
            {
                if(arg == true){
                    Context.SelectedRowsToCreate.Add(item0);
                } else {
                    Context.SelectedRowsToCreate.Remove(item0);
                }
            }


        }
    }
}
