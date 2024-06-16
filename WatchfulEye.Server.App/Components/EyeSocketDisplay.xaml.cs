using System.Windows.Controls;
using WatchfulEye.Server.Eyes;

namespace WatchfulEye.Server.App.Components;

public partial class EyeSocketDisplay : UserControl {
    private readonly EyeSocketModelController _modelController;
    
    public EyeSocketDisplay() {
        InitializeComponent();
        _modelController = (EyeSocketModelController)DataContext;
    }

    public void AssignEye(EyeSocket eye) => _modelController.ActivateEye(eye);

    public void UnassignEye() => _modelController.DeactivateEye();
}