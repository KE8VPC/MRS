using System.Windows.Controls;
using System.Windows.Input;

namespace MRS;

internal static class SliderWheel
{
    internal static void Enable(Slider slider,double step,bool extraFine=true,Action? changed=null)
    {
        int remainder=0;
        slider.SmallChange=step;
        slider.ToolTip="Mouse wheel: fine adjustment"+(extraFine?"; Shift + wheel: 10× finer":"")+". Hover outside the slider to scroll the panel.";
        slider.MouseLeave+=(_,_)=>remainder=0;
        slider.PreviewMouseWheel+=(_,e)=>
        {
            if(!slider.IsEnabled)return;
            e.Handled=true;
            remainder+=e.Delta;
            int ticks=remainder/120;remainder%=120;
            if(ticks==0)return;
            double increment=step*(extraFine&&Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?.1:1);
            double value=Math.Clamp(Math.Round(slider.Value+ticks*increment,8),slider.Minimum,slider.Maximum);
            if(value==slider.Value)return;
            slider.SetCurrentValue(Slider.ValueProperty,value);
            changed?.Invoke();
        };
    }
}
