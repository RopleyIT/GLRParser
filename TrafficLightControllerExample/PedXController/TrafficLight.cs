using System;

namespace PedXController
{
    public enum LightColour
    {
        RED,
        YELLOW,
        GREEN,
        REDANDYELLOW
    }

    /// <summary>
    /// Simple simulation of a traffic light
    /// </summary>

    public class TrafficLight
    {
        public TrafficLight() => Colour = LightColour.GREEN;

        public event EventHandler PropertyChanged;

        private LightColour colour;
        public LightColour Colour
        {
            get => colour;
            set
            {
                colour = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, EventArgs.Empty);
            }
        }

        public bool Red => Colour == LightColour.RED || Colour == LightColour.REDANDYELLOW;

        public bool Yellow => Colour == LightColour.REDANDYELLOW || Colour == LightColour.YELLOW;

        public bool Green => Colour == LightColour.GREEN;
    }
}
