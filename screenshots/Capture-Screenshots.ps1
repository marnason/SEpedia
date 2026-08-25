Add-Type -AssemblyName System.Drawing

if (-not ('NativeScreenshotMethods' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class NativeScreenshotMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr windowHandle, out Rect rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

}
'@
}

if (-not ('NativeDpiMethods' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class NativeDpiMethods
{
    [DllImport("user32.dll")]
    public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);
}
'@
}

if (-not ('NativeMouseInputMethods' -as [type])) {
    Add-Type @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class NativeMouseInputMethods
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    public static void SendMouseButton(uint flags)
    {
        Input[] inputs = new Input[1];
        inputs[0].Type = 0;
        inputs[0].Data.Mouse.Flags = flags;

        if (SendInput(1, inputs, Marshal.SizeOf(typeof(Input))) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}
'@
}

# Keep window bounds, cursor positions, and screen captures in physical pixels,
# even when Windows display scaling is greater than 100%.
$perMonitorAwareV2 = [IntPtr]::new(-4)
[void][NativeDpiMethods]::SetThreadDpiAwarenessContext($perMonitorAwareV2)

function Get-SpaceEngineersWindowPosition {
    $game = Get-Process -Name 'SpaceEngineers' -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero } |
        Select-Object -First 1

    if ($null -eq $game) {
        throw 'Could not find an open Space Engineers window.'
    }

    $windowRectangle = [NativeScreenshotMethods+Rect]::new()
    if (-not [NativeScreenshotMethods]::GetWindowRect($game.MainWindowHandle, [ref]$windowRectangle)) {
        throw 'Could not determine the position of the Space Engineers window.'
    }

    [void][NativeScreenshotMethods]::SetForegroundWindow($game.MainWindowHandle)
    Start-Sleep -Milliseconds 200

    return @($windowRectangle.Left, $windowRectangle.Top)
}

function Click-GamePosition {
    param(
        [Parameter(Mandatory)]
        [int]$X,

        [Parameter(Mandatory)]
        [int]$Y
    )

    $gamePosition = Get-SpaceEngineersWindowPosition
    if (-not [NativeScreenshotMethods]::SetCursorPos(
        ($gamePosition[0] + $X),
        ($gamePosition[1] + $Y)
    )) {
        throw 'Could not move the mouse pointer to the click position.'
    }

    Start-Sleep -Milliseconds 50
    [NativeMouseInputMethods]::SendMouseButton(0x0002)
    Start-Sleep -Milliseconds 50
    [NativeMouseInputMethods]::SendMouseButton(0x0004)
    Start-Sleep -Milliseconds 200
}

function Move-GameCursor {
    param(
        [Parameter(Mandatory)]
        [int]$X,

        [Parameter(Mandatory)]
        [int]$Y
    )

    $gamePosition = Get-SpaceEngineersWindowPosition
    if (-not [NativeScreenshotMethods]::SetCursorPos(
        ($gamePosition[0] + $X),
        ($gamePosition[1] + $Y)
    )) {
        throw 'Could not move the mouse pointer to the requested position.'
    }

    Start-Sleep -Milliseconds 200
}

function Save-GameScreenshot {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$FileName
    )

    # Fixed framing for every screenshot, relative to the game window.
    $captureOffsetX = 640
    $captureOffsetY = 360
    $captureWidth = 2560
    $captureHeight = 1440

    if ([System.IO.Path]::GetFileName($FileName) -ne $FileName) {
        throw 'FileName must be a filename, not a path.'
    }

    if ([System.IO.Path]::GetExtension($FileName) -ne '.png') {
        $FileName = "$FileName.png"
    }

    $gamePosition = Get-SpaceEngineersWindowPosition
    $outputPath = Join-Path -Path $PSScriptRoot -ChildPath $FileName
    $bitmap = [System.Drawing.Bitmap]::new($captureWidth, $captureHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.CopyFromScreen(
            ($gamePosition[0] + $captureOffsetX),
            ($gamePosition[1] + $captureOffsetY),
            0,
            0,
            [System.Drawing.Size]::new($captureWidth, $captureHeight)
        )

        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    Get-Item -LiteralPath $outputPath
}

Start-Sleep -Milliseconds 2000
Click-GamePosition 1200 450
Start-Sleep -Milliseconds 400
Click-GamePosition 800 1120
Start-Sleep -Milliseconds 400
Move-GameCursor 2800 1080
Start-Sleep -Milliseconds 400
Save-GameScreenshot '01-ingots.png'


Start-Sleep -Milliseconds 400
Click-GamePosition 2800 1080
Start-Sleep -Milliseconds 400
Move-GameCursor 3300 1900
Start-Sleep -Milliseconds 400
Save-GameScreenshot '02-rocket.png'
