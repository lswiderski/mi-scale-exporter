using Android.App;
using Android.OS;
using Android.Widget;

namespace MiScaleExporter.MAUI
{
    [Activity(Name = "com.lukaszswiderski.MiScaleExporter.PermissionsRationaleActivity", Exported = true)]
    [IntentFilter(new[] { "androidx.health.ACTION_SHOW_PERMISSIONS_RATIONALE" })]
    public class PermissionsRationaleActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var text = new TextView(this)
            {
                Text = "MiScale Exporter writes body measurements that you record with your Bluetooth scale " +
                       "(weight, body fat percentage, bone mass, body water mass, lean body mass and basal " +
                       "metabolic rate) to Google Health Connect, so they appear in your health and fitness " +
                       "apps. No data is read from Health Connect and nothing is shared with third parties."
            };

            var pad = (int)(24 * Resources.DisplayMetrics.Density);
            text.SetPadding(pad, pad, pad, pad);

            var scroll = new Android.Widget.ScrollView(this);
            scroll.AddView(text);
            SetContentView(scroll);
        }
    }
}
