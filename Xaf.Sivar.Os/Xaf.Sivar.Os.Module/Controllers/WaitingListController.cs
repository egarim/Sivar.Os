using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Xaf.Sivar.Os.Module.Controllers
{
    /// <summary>
    /// Controller for managing waiting list entries in XAF admin backend.
    /// Provides approve, reject, and batch approval actions.
    /// </summary>
    public class WaitingListController : ObjectViewController<ListView, WaitingListEntry>
    {
        private SimpleAction approveAction;
        private SimpleAction rejectAction;
        private PopupWindowShowAction approveNextAction;
        private SimpleAction refreshStatsAction;

        public WaitingListController()
        {
            // Approve Selected Action
            approveAction = new SimpleAction(this, "ApproveWaitingListEntry", PredefinedCategory.Edit)
            {
                Caption = "Approve",
                ToolTip = "Approve selected users and grant them access to the app",
                ImageName = "Action_Grant",
                SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
                ConfirmationMessage = "Approve selected users? They will be granted access to the app."
            };
            approveAction.Execute += ApproveAction_Execute;

            // Reject Selected Action
            rejectAction = new SimpleAction(this, "RejectWaitingListEntry", PredefinedCategory.Edit)
            {
                Caption = "Reject",
                ToolTip = "Reject selected users",
                ImageName = "Action_Deny",
                SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
                ConfirmationMessage = "Reject selected users? They will be removed from the waiting list."
            };
            rejectAction.Execute += RejectAction_Execute;

            // Approve Next N Action
            approveNextAction = new PopupWindowShowAction(this, "ApproveNextInQueue", PredefinedCategory.Tools)
            {
                Caption = "Approve Next...",
                ToolTip = "Approve the next N users in the queue",
                ImageName = "Action_Grant"
            };
            approveNextAction.CustomizePopupWindowParams += ApproveNextAction_CustomizePopupWindowParams;
            approveNextAction.Execute += ApproveNextAction_Execute;

            // Refresh Stats Action
            refreshStatsAction = new SimpleAction(this, "RefreshWaitingListStats", PredefinedCategory.View)
            {
                Caption = "Show Stats",
                ToolTip = "Show waiting list statistics",
                ImageName = "BO_StateMachine"
            };
            refreshStatsAction.Execute += RefreshStatsAction_Execute;
        }

        private IWaitingListService? GetWaitingListService()
        {
            return Application?.ServiceProvider?.GetService<IWaitingListService>();
        }

        private string GetAdminName()
        {
            return SecuritySystem.CurrentUserName ?? "admin";
        }

        private async void ApproveAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var service = GetWaitingListService();
            if (service == null)
            {
                ShowError("Waiting list service not available");
                return;
            }

            var selectedEntries = e.SelectedObjects.Cast<WaitingListEntry>().ToList();
            var adminName = GetAdminName();
            var approvedCount = 0;

            foreach (var entry in selectedEntries)
            {
                if (entry.Status == WaitingListStatus.Waiting)
                {
                    var success = await service.ApproveUserAsync(entry.UserId, adminName);
                    if (success) approvedCount++;
                }
            }

            // Refresh the view
            ObjectSpace.Refresh();
            View.Refresh();

            ShowMessage($"Approved {approvedCount} of {selectedEntries.Count} users");
        }

        private async void RejectAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var service = GetWaitingListService();
            if (service == null)
            {
                ShowError("Waiting list service not available");
                return;
            }

            var selectedEntries = e.SelectedObjects.Cast<WaitingListEntry>().ToList();
            var rejectedCount = 0;

            foreach (var entry in selectedEntries)
            {
                if (entry.Status != WaitingListStatus.Approved && entry.Status != WaitingListStatus.Rejected)
                {
                    var success = await service.RejectUserAsync(entry.UserId, "Rejected by admin");
                    if (success) rejectedCount++;
                }
            }

            // Refresh the view
            ObjectSpace.Refresh();
            View.Refresh();

            ShowMessage($"Rejected {rejectedCount} of {selectedEntries.Count} users");
        }

        private void ApproveNextAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            // Create a simple parameter object for entering the count
            var os = Application.CreateObjectSpace(typeof(ApproveNextParams));
            var parameters = os.CreateObject<ApproveNextParams>();
            parameters.Count = 10; // Default to 10

            e.View = Application.CreateDetailView(os, parameters);
            e.DialogController.AcceptAction.Caption = "Approve";
        }

        private async void ApproveNextAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var service = GetWaitingListService();
            if (service == null)
            {
                ShowError("Waiting list service not available");
                return;
            }

            var parameters = e.PopupWindowViewCurrentObject as ApproveNextParams;
            if (parameters == null || parameters.Count <= 0)
            {
                ShowError("Invalid count specified");
                return;
            }

            var adminName = GetAdminName();
            var approved = await service.ApproveNextInQueueAsync(parameters.Count, adminName);

            // Refresh the view
            ObjectSpace.Refresh();
            View.Refresh();

            ShowMessage($"Approved {approved} users from the queue");
        }

        private async void RefreshStatsAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var service = GetWaitingListService();
            if (service == null)
            {
                ShowError("Waiting list service not available");
                return;
            }

            var stats = await service.GetStatsAsync();

            var message = $@"Waiting List Statistics:
━━━━━━━━━━━━━━━━━━━━━━━
Total Signups: {stats.TotalSignups}
Pending Verification: {stats.PendingVerification}
Waiting for Approval: {stats.WaitingApproval}
Approved (Total): {stats.ApprovedTotal}
Approved (Today): {stats.ApprovedToday}
Approved (This Week): {stats.ApprovedThisWeek}
Rejected: {stats.RejectedTotal}

Top Countries:
{string.Join("\n", stats.SignupsByCountry.OrderByDescending(x => x.Value).Take(5).Select(x => $"  {x.Key}: {x.Value}"))}";

            ShowMessage(message);
        }

        private void ShowMessage(string message)
        {
            Application.ShowViewStrategy.ShowMessage(message, InformationType.Success);
        }

        private void ShowError(string message)
        {
            Application.ShowViewStrategy.ShowMessage(message, InformationType.Error);
        }
    }

    /// <summary>
    /// Parameters for the "Approve Next" popup
    /// </summary>
    [DomainComponent]
    public class ApproveNextParams
    {
        public int Count { get; set; } = 10;
    }
}
