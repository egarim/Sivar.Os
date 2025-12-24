using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;

namespace Xaf.Sivar.Os.Module.Controllers
{
    /// <summary>
    /// Controller to handle navigation to the SeederLog singleton.
    /// When the SeederLog navigation item is clicked, this controller ensures
    /// the singleton instance is loaded and displayed in a DetailView.
    /// </summary>
    public class SeederLogNavigationController : WindowController
    {
        private const string SeederLogNavItemId = "SeederLog";

        public SeederLogNavigationController()
        {
            TargetWindowType = WindowType.Main;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            
            var showNavigationItemController = Frame.GetController<ShowNavigationItemController>();
            if (showNavigationItemController != null)
            {
                showNavigationItemController.CustomShowNavigationItem += ShowNavigationItemController_CustomShowNavigationItem;
            }
        }

        protected override void OnDeactivated()
        {
            var showNavigationItemController = Frame.GetController<ShowNavigationItemController>();
            if (showNavigationItemController != null)
            {
                showNavigationItemController.CustomShowNavigationItem -= ShowNavigationItemController_CustomShowNavigationItem;
            }
            
            base.OnDeactivated();
        }

        private void ShowNavigationItemController_CustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
        {
            // Check if the navigation item is for SeederLog
            if (e.ActionArguments.SelectedChoiceActionItem?.Id == SeederLogNavItemId ||
                e.ActionArguments.SelectedChoiceActionItem?.Data?.ToString() == "SeederLog_DetailView")
            {
                // Create or get the singleton instance
                IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(BusinessObjects.SeederLog));
                
                // Get the singleton (should only be one)
                var seederLogs = objectSpace.GetObjects<BusinessObjects.SeederLog>();
                BusinessObjects.SeederLog seederLog;
                
                if (seederLogs.Count == 0)
                {
                    // Create if doesn't exist (shouldn't happen if Updater ran)
                    seederLog = objectSpace.CreateObject<BusinessObjects.SeederLog>();
                    seederLog.LogText = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] SeederLog created on first access.\n";
                    seederLog.LastOperationSummary = "Created via navigation";
                    seederLog.LastOperationAt = DateTime.UtcNow;
                    objectSpace.CommitChanges();
                }
                else
                {
                    seederLog = seederLogs[0];
                }
                
                // Create the DetailView for the singleton
                DetailView detailView = Application.CreateDetailView(objectSpace, seederLog);
                detailView.ViewEditMode = ViewEditMode.Edit;
                
                // Set the view parameters
                e.ActionArguments.ShowViewParameters.CreatedView = detailView;
                e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
                e.Handled = true;
            }
        }
    }
}
