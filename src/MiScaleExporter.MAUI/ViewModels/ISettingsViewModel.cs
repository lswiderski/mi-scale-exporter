using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface ISettingsViewModel
    {
        void SexRadioButtonChanged(object s, CheckedChangedEventArgs e);
        void ScaleTypeRadioButton_Changed(object s, CheckedChangedEventArgs e);
        Task LoadPreferencesAsync();
    }
}
