using System;
using System.Collections.Generic;
using System.Text;
 

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface IScaleViewModel
    {
        Task CheckPreferencesAsync();
        Task LoadPreferencesAsync();

        bool IsGuestMode { get; set; }
        string GuestAge { get; set; }
        string GuestHeight { get; set; }
        MiScaleExporter.Models.Sex GuestSex { get; set; }
        int GuestSexIndex { get; set; }
        Command GuestScanCommand { get; }
    }
}
