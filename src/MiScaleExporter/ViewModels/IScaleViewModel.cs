using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MiScaleExporter.ViewModels
{
    public interface IScaleViewModel
    {
        void SexRadioButtonChanged(object s, CheckedChangedEventArgs e);
        void ScaleTypeRadioButton_Changed(object s, CheckedChangedEventArgs e);
    }
}
