using System;

namespace Skoofidons.SharedSettings.V1
{
    // Logical window coordinates; kept independent of Unity for layout verification.
    public sealed class MenuFooterLayout
    {
        public float FooterHeight {get;private set;}
        public float ButtonsY {get;private set;}
        public float CloseX {get;private set;}
        public float CloseWidth {get;private set;}
        public float ResetX {get;private set;}
        public float ResetWidth {get;private set;}
        public float ApplyX {get;private set;}
        public float ApplyWidth {get;private set;}
        public float StatusY {get;private set;}
        public float StatusWidth {get;private set;}
        public float StatusHeight {get;private set;}
        public int ButtonFontSize {get;private set;}

        public static MenuFooterLayout Create(float width,float height,bool reset,bool apply,bool russian)
        {
            float closeWidth=125,resetWidth=reset?(russian?200:226):0,applyWidth=apply?(russian?245:185):0;
            int count=1+(reset?1:0)+(apply?1:0);
            float gap=12,scale=Math.Min(1,Math.Max(1,width-36-gap*(count-1))/(closeWidth+resetWidth+applyWidth));
            closeWidth*=scale;resetWidth*=scale;applyWidth*=scale;
            float x=width-18-closeWidth-resetWidth-applyWidth-gap*(count-1);
            bool stacked=width<1100;
            var layout=new MenuFooterLayout{
                FooterHeight=stacked?132:96,ButtonsY=height-59,
                CloseX=x,CloseWidth=closeWidth,ResetWidth=resetWidth,ApplyWidth=applyWidth,
                StatusY=stacked?height-126:height-69,StatusHeight=stacked?30:58,
                StatusWidth=stacked?width-40:Math.Max(50,x-35),
                ButtonFontSize=(int)Math.Max(12,20*scale)
            };
            x+=closeWidth+gap;
            if(reset){layout.ResetX=x;x+=resetWidth+gap;}
            if(apply)layout.ApplyX=x;
            return layout;
        }
    }
}
