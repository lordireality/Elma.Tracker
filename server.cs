using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EleWise.ELMA.API;
using EleWise.ELMA.DataClasses;
using EleWise.ELMA.Extensions;
using EleWise.ELMA.Model.Common;
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

            /// <summary>
            /// Сценарий действия "SelPlanItemsAction"
            /// </summary>
            public PlanRowDTO[] GetPlanItems(SelPlanDto arg)
            {
                
                var trackerObject = PublicAPI.Objects.UserObjects.UserPlanTracker.Load(arg.PlanId);
                //arg.State
                string EQLFilter = string.Format("(Tracker = {0})",arg.PlanId);
                if(arg.State != "Все"){
                    EQLFilter += string.Format(" and (Status = '{0}')",arg.State);
                }
                EQLFilter += string.Format("and (StartDate > DateTime({0}, 1, 1)) and (EndDate < DateTime({0}, 12, 31))",arg.Year);
                var planItems = PublicAPI.Objects.UserObjects.UserTrackerItem.Find(EQLFilter);
                List<Planner.PlanRowDTO> dtoItems = new List<PlanRowDTO>();
                foreach(var item in planItems){
                   
                    var newItem = new PlanRowDTO();
                    newItem.RecordId = item.Id;
                    newItem.Name = item.Name;
                    newItem.EndDate = item.EndDate;
                    newItem.StartDate = item.StartDate;
                    if(item.RelDoc.Count != 0){ 
                        newItem.RelDoc.AddAll(item.RelDoc);
                    }
                    if(item.RelProcess.Count != 0){ 
                        newItem.RelProcess.AddAll(item.RelProcess);
                    }
                    if(item.RelProject.Count != 0){ 
                        newItem.RelProject.AddAll(item.RelProject);
                    }
                    if(item.RelTask.Count != 0){ 
                        newItem.RelTask.AddAll(item.RelTask);
                    }
                    if(item.Executors.Count != 0){ 
                        newItem.Executors.AddAll(item.Executors);
                    }
                    if(item.EndDateFact != null){
                        newItem.EndFact = item.EndDateFact;
                        if(item.StartDateFact != null){
                            newItem.StartFact = item.StartDateFact.Value;
                        } else {
                            newItem.StartFact = item.StartDate;
                        }
                    }


                    newItem.Status = item.Status != null ? item.Status.Value : "";
                    if(!string.IsNullOrEmpty(item.Category)){
                        var label = trackerObject.TrackerElemCategory.Where(c=>c.Name == item.Category).Select(c=> new {c.ColorCode, c.Name}).FirstOrDefault();
                        newItem.LabelColorCode =  label != null ? label.ColorCode : "#FF0000";
                        newItem.LabelName = label != null ? label.Name : "Без категории";
                    } else {
                        newItem.LabelColorCode = "#FF0000";
                        newItem.LabelName = "Без категории";
                    }
                    
                    dtoItems.Add(newItem);
                } //LabelColorCode
                return dtoItems.ToArray();
            }

            /// <summary>
            /// Сценарий действия "CheckIsAdminCurrentUser"
            /// </summary>
            public bool CheckIsAdminCurrentUser()
            {
                return PublicAPI.Portal.Security.UserGroup.CheckUserInGroup(PublicAPI.Portal.Security.User.GetCurrentUser(), PublicAPI.Portal.Security.UserGroup.Load(1));
            }


            /// <summary>
            /// Сценарий действия "CheckRightsAction"
            /// </summary>
            public AccessDTO CheckAccessRightsToPlan(long arg)
            {
                var trackerObject = PublicAPI.Objects.UserObjects.UserPlanTracker.Load(arg);
                if(trackerObject != null){
                    var currentUser = PublicAPI.Portal.Security.User.GetCurrentUser();
                    if(trackerObject.Manager.Contains(currentUser)){
                        return new AccessDTO(){ View = true, Edit = true};
                    } else {
                        var orgCheck = trackerObject.AccessOrgItems.Any(c=>c.User == currentUser || c.Users.Contains(currentUser));
                        var userCheck = trackerObject.AccessUsers.Any(c=>c == currentUser);
                        bool groupCheck = false;
                        foreach(var grp in trackerObject.AccessGroups){
                            groupCheck = groupContainUserRecursive(grp, currentUser) == false ? groupCheck : true;
                        }
                        return new AccessDTO(){ View = orgCheck || userCheck || groupCheck, Edit = false};
                    }
                } else {
                    return new AccessDTO(){ View = false, Edit = false};
                }
            }



            public bool groupContainUserRecursive(EleWise.ELMA.Security.Models.UserGroup curentGroup, EleWise.ELMA.Security.Models.User user){
                bool isContainsOrgs = curentGroup.OrganizationItems.Any(c=>c.User == user || c.Users.Contains(user));
                bool isContainsUsers = curentGroup.Users.Contains(user);
                bool isContainsGroups = false;
                foreach(var curgroup in curentGroup.Groups){
                    isContainsGroups = groupContainUserRecursive(curgroup, user) == false ? isContainsGroups : true ;
                }
                return isContainsOrgs || isContainsGroups || isContainsUsers;
            }

            /// <summary>
            /// Сценарий действия "EditSaveAction"
            /// </summary>
            public bool EditSave(PlanRowDTO arg)
            {
                var record = PublicAPI.Objects.UserObjects.UserTrackerItem.Load(arg.RecordId);
                if(record == null){return false; }
                record.Name = arg.Name;
                record.EndDate = arg.EndDate;
                record.StartDate = arg.StartDate;
                record.StartDateFact = arg.StartFact != null ? arg.StartFact : null;
                record.EndDateFact = arg.EndFact != null ? arg.EndFact : null;
                record.Category = arg.LabelName;
                record.Status = new DropDownItem(arg.Status);
                
                record.Executors.Clear();
                foreach(var item in arg.Executors){
                    record.Executors.Add(item);
                }
                
                record.RelProcess.Clear();
                foreach(var item in arg.RelProcess){
                    record.RelProcess.Add(item);
                }
                record.RelTask.Clear();
                foreach(var item in arg.RelTask){
                    record.RelTask.Add(item);
                }
                record.RelDoc.Clear();
                foreach(var item in arg.RelDoc){
                    record.RelDoc.Add(item);
                }
                record.RelProject.Clear();
                foreach(var item in arg.RelProject){
                    record.RelProject.Add(item);
                }
                record.Save();
                return true;
            }

            /// <summary>
            /// Сценарий действия "RemoveAction"
            /// </summary>
            public bool RemovePlanItem(long arg)
            {
                var item = PublicAPI.Objects.UserObjects.UserTrackerItem.Load(arg);
                item.Tracker = null;
                item.Delete();
                return true;
            }

            /// <summary>
            /// Сценарий действия "CreateSaveAction"
            /// </summary>
            public bool SaveNewRows(CreateTrackerItem[] arg)
            {
                //string EQLFilter = string.Format("(Tracker = {0})",arg.PlanId);
                foreach(var item in arg){
                    var trackerObject = PublicAPI.Objects.UserObjects.UserPlanTracker.Load(item.PlanId);
                    var record = PublicAPI.Objects.UserObjects.UserTrackerItem.Create();
                    record.Tracker = trackerObject;
                    record.Name = item.Name;
                    record.EndDate = item.EndDate;
                    record.StartDate = item.StartDate;
                    record.StartDateFact = item.StartDateFact != null ? item.StartDateFact : null;
                    record.EndDateFact = item.EndDateFact != null ? item.EndDateFact : null;
                    record.Category = item.Label;
                    record.Status = new DropDownItem(item.State.Value);
                    foreach(var rec in item.Executors){
                        record.Executors.Add(rec);
                    }
                    foreach(var rec in item.RelatedProc){
                        record.RelProcess.Add(rec);
                    }
                    foreach(var rec in item.RelTasks){
                        record.RelTask.Add(rec);
                    }
                    foreach(var rec in item.RelDocs){
                        record.RelDoc.Add(rec);
                    }
                    foreach(var rec in item.RelProjects){
                        record.RelProject.Add(rec);
                    }
                    record.Save();
                }
                return true;
            }


        }
    }
}
