# Glitch Chop

A vocal chop sampler for Windows. Load a vocal, slice it up, and hit **Randomize** to turn the slices into glitchy melodies with random pitch, speed, stutters, reverses, filters and tape stops.

## Download

Grab **GlitchChop.exe** from the [latest release](https://github.com/codypowpow-afk/glitch-chop/releases/latest) and run it. There's nothing to install: it's one file that opens in its own window.

- Needs Windows 10 or 11 with the Microsoft Edge WebView2 Runtime. Windows 11 has it built in. If it's missing, the app offers Microsoft's download page.
- The exe isn't code-signed, so Windows SmartScreen may warn the first time. Click **More info → Run anyway**.

## Features

**Chop modes**
- **Grid**: slice by tempo and time signature (2/4–7/8), every bar, beat, ½, ⅓ (triplet) or ¼ beat, with a start offset. **Fit BPM to length** guesses the tempo from the file length.
- **Transients**: cut where the volume jumps (syllables, consonants), with sensitivity and minimum gap.
- **Notes**: pitch-tracks the voice and cuts each time the sung note changes; slices are labeled with their note.
- **Equal**: 2–64 even slices.

**Glitch engine**: Randomize, Mutate (re-roll a third of the steps), or click one step to re-roll it. You can set tempo, steps, pitch range, scale (chromatic, major, minor, pentatonic, root + 5th, octaves, free/detuned), speed jumps, stutter, reverse, rests, gate and echo. Turn on "New pattern every loop" for nonstop chaos.

**FX rack** (rolled per step)
- **Filter**: low-, high- or band-pass with resonance, per-step cutoff jitter, and a sweep that zaps each hit from bright to dark.
- **Bitcrusher**: bit depth and downsampling on a random share of steps.
- **Motion**: pitch glides up or down, tape stops, and stereo pan spread.

**Export**: save the melody as a WAV file (1–8 loops), or every chop as WAVs in a zip.

Keys: <kbd>Space</kbd> play/stop · <kbd>R</kbd> randomize · <kbd>M</kbd> mutate · <kbd>1</kbd>–<kbd>9</kbd> play chops.

Pitch shifting is tape-style: higher notes also play faster.

## Build it yourself

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

The build needs only Windows. It uses the C# compiler that ships with .NET Framework 4.x, downloads the WebView2 SDK from nuget.org on first run, and checks its Microsoft signatures. It writes:

- `dist\GlitchChop.exe`: the desktop app, with the page and WebView2 libraries embedded
- `dist\index.html`: the same app as a single web page

## Layout

| Path | What it is |
|---|---|
| `glitch-chop.html` | The whole app (UI + Web Audio engine) |
| `desktop/GlitchChop.cs` | Native Windows shell (WinForms + WebView2) |
| `desktop/make-icon.ps1` | Regenerates `desktop/app.ico` |
| `build.ps1` | Builds the exe and standalone page |
