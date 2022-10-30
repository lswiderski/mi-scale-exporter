using System;
using System.Collections.Generic;
using System.Text;
 

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface IScaleViewModel
    {
        Task CheckPreferencesAsync();
        Task LoadPreferencesAsync();
    }
}
