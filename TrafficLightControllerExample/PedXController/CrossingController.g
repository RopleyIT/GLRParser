options
{
	namespace PedXController,
	fsmclass CrossingController
}

events
{
    TIMERTICK,
	BUTTONPRESS
}

guards
{
	ButtonWasPressed
    {
        return WalkIndicator.ButtonPressed;
    }
}

fsm(Green)
{
	// The AllRed state is used when both traffic and
	// pedestrians are barred from crossing. This is
	// the state the controller is in while waiting
	// for slower pedestrians to complete their crossing.

	AllRed: 
		TIMERTICK RedAndYellow
		{
            SetLightRedAndYellow();
            SetRedAndYellowTimer();
        }
	|	BUTTONPRESS AllRed
		{
            RecordButtonPressed();
        }
	;

	// This is an English traffic light! In England
	// the lights go to both red and yellow lit up
	// for a few seconds between being red and green.

	RedAndYellow: 
		TIMERTICK TimedGreen
        {
			SetLightGreen();
            SetGreenTimer();
        }
	|	BUTTONPRESS RedAndYellow
        {
			RecordButtonPressed();
        }
	;

	// Once the lights have gone green, there is a
	// minimum interval they must remain green before
	// the controller will respond to the next button press.

	TimedGreen: 
		TIMERTICK[ButtonWasPressed] Yellow
        {
			SetLightYellow();
            SetYellowTimer();
        }
	|	BUTTONPRESS TimedGreen
        {
			RecordButtonPressed();
        }
	|	TIMERTICK[!ButtonWasPressed] Green
	;

	// After the minimum interval for the lights on green
	// has expired, the lights will stay on green indefinitely
	// unless a pedestrian presses the request to cross button.

	Green: 
		BUTTONPRESS Yellow
        {
			SetLightYellow();
            RecordButtonPressed();
            SetYellowTimer();
        }
	;

	// The yellow light is displayed for a few seconds between
	// green and red to tell traffic to stop at the light if
	// it is safe for them to do so.

	Yellow: 
		BUTTONPRESS Yellow
        {
			RecordButtonPressed();
        }
	|	TIMERTICK RedWalk
        {
			SetLightRed();
            SetLightWalk();
            ClearButtonPress();
            SetWalkTimer();
        }
	;

	// Once the lights have gone to red, we allow
	// pedestrians to cross the road.

	RedWalk: 
        BUTTONPRESS RedWalk
	|	TIMERTICK AllRed
        {
			SetLightDontWalk();
            SetAllRedTimer();
        }
	;
}