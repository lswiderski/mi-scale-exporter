using System;
using System.Collections.Generic;
using System.Text;
 

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface IScaleViewModel
    {
        void SexRadioButtonChanged(object s, CheckedChangedEventArgs e);
        void ScaleTypeRadioButton_Changed(object s, CheckedChangedEventArgs e);
        void CheckPreferences();
    }
}
