using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;

namespace Xaf.Sivar.Os.Module.Controllers
{
    /// <summary>
    /// Controller to handle navigation to the KeycloakSetupLog singleton.
    /// When the KeycloakSetupLog navigation item is clicked, this controller ensures
    /// the singleton instance is loaded and displayed in a DetailView.
    /// </summary>
    public class KeycloakSetupLogNavigationController : WindowController
    {
        private const string KeycloakSetupLogNavItemId = "KeycloakSetupLog";

        public KeycloakSetupLogNavigationController()
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
            // Check if the navigation item is for KeycloakSetupLog
            if (e.ActionArguments.SelectedChoiceActionItem?.Id == KeycloakSetupLogNavItemId ||
                e.ActionArguments.SelectedChoiceActionItem?.Data?.ToString() == "KeycloakSetupLog_DetailView")
            {
                // Create or get the singleton instance
                IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(BusinessObjects.KeycloakSetupLog));
                
                // Get the singleton (should only be one)
                var logs = objectSpace.GetObjects<BusinessObjects.KeycloakSetupLog>();
                BusinessObjects.KeycloakSetupLog log;
                
                if (logs.Count == 0)
                {
                    // Create if doesn't exist
                    log = objectSpace.CreateObject<BusinessObjects.KeycloakSetupLog>();
                    log.LogText = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] KeycloakSetupLog created on first access.\n";
                    objectSpace.CommitChanges();
                }
                else
                {
                    log = logs.First();
                }

                // Create and show the DetailView
                var detailView = Application.CreateDetailView(objectSpace, log, false);
                detailView.ViewEditMode = ViewEditMode.Edit;
                
                e.ActionArguments.ShowViewParameters.CreatedView = detailView;
                e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
                e.Handled = true;
            }
        }
    }
}
