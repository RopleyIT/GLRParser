using System;

namespace PedXController
{
    public class StopWalkIndicator
    {
        public event EventHandler PropertyChanged;

        public StopWalkIndicator() => CanWalk = false;

        private bool buttonPressed;
        public bool ButtonPressed
        {
            get => buttonPressed;
            set
            {
                buttonPressed = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, EventArgs.Empty);
            }
        }

        private bool canWalk;
        public bool CanWalk
        {
            get => canWalk;
            set
            {
                canWalk = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, EventArgs.Empty);
            }
        }

        public bool Walk => CanWalk;

        public bool Stop => !CanWalk;
    }
}
