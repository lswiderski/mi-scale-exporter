using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.MAUI.ViewModels
{
    public interface ISettingsViewModel
    {
        void SexRadioSetToMale();
        void SexRadioSetToFemale();
        void ScaleTypeSetToBodyCompositionScale();
        void ScaleTypeSetToMiscale();
        Task LoadPreferencesAsync();
    }
}
