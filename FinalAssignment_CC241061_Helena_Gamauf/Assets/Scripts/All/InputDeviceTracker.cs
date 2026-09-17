using UnityEngine.InputSystem;

public static class InputDeviceTracker
{
    const float StickDeadzoneSqr = 0.04f;
    const float MouseDeltaDeadzoneSqr = 0.25f;

    public static bool IsUsingGamepad { get; private set; }

    public static void UpdateFromInput()
    {
        if (HasKeyboardMouseInput())
        {
            IsUsingGamepad = false;
            return;
        }

        if (HasGamepadInput())
        {
            IsUsingGamepad = true;
        }
    }

    static bool HasGamepadInput()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return false;
        }

        if (gamepad.leftStick.ReadValue().sqrMagnitude > StickDeadzoneSqr
            || gamepad.rightStick.ReadValue().sqrMagnitude > StickDeadzoneSqr
            || gamepad.dpad.ReadValue().sqrMagnitude > StickDeadzoneSqr)
        {
            return true;
        }

        if (gamepad.leftTrigger.ReadValue() > 0.1f || gamepad.rightTrigger.ReadValue() > 0.1f)
        {
            return true;
        }

        return gamepad.buttonSouth.isPressed
            || gamepad.buttonNorth.isPressed
            || gamepad.buttonEast.isPressed
            || gamepad.buttonWest.isPressed
            || gamepad.leftShoulder.isPressed
            || gamepad.rightShoulder.isPressed
            || gamepad.startButton.isPressed
            || gamepad.selectButton.isPressed;
    }

    static bool HasKeyboardMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.delta.ReadValue().sqrMagnitude > MouseDeltaDeadzoneSqr
                || mouse.scroll.ReadValue().sqrMagnitude > 0.01f)
            {
                return true;
            }

            if (mouse.leftButton.isPressed
                || mouse.rightButton.isPressed
                || mouse.middleButton.isPressed
                || mouse.forwardButton.isPressed
                || mouse.backButton.isPressed)
            {
                return true;
            }
        }

        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.isPressed;
    }
}
