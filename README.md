# Glitch Chop

A vocal chop instrument for Windows. Load a vocal and it finds the notes and key, slices it, and turns the slices into melodies that stay in key and on the beat. Turn up Chaos when you want it to fall apart.

## Download

Grab **GlitchChop.exe** from the [latest release](https://github.com/codypowpow-afk/glitch-chop/releases/latest) and run it. There's nothing to install: it's one file that opens in its own window.

- Needs Windows 10 or 11 with the Microsoft Edge WebView2 Runtime. Windows 11 has it built in. If it's missing, the app offers Microsoft's download page.
- The exe isn't code-signed, so Windows SmartScreen may warn the first time. Click **More info → Run anyway**.

## How the melodies work

- **Tuned chops:** the app works out the note each chop already sings and shifts it to the note the melody needs, so everything lands in key.
- **Key detection:** it detects the sample's key, or you can pick the key and scale yourself.
- **Real phrases:** each pattern repeats a short motif over a chord progression. Strong beats land on chord tones and the phrase resolves at the end.
- **Fixed bars:** patterns are 1, 2 or 4 bars of 4/4 on a 16th-note grid, using a groove pattern, so loops always line up.
- **Sustain:** each chop gets a seamless loop at its steadiest point, so short chops can hold long notes.

## Features

**Chop modes**
- **Notes** (default): pitch-tracks the voice and cuts each time the sung note changes.
- **Transients**: cuts where the volume jumps (syllables, consonants).
- **Grid**: slices by tempo and time signature (2/4–7/8). **Fit BPM to length** guesses the tempo from the file length.
- **Equal**: 2–64 even slices.

**Vibes**: Vocal hook, Dreamy sustain, Future garage, House stabs, Trap rolls and Full glitch. Each sets the groove and sliders as a starting point.

**Melody engine**: Randomize, Mutate (rewrites a third of the notes, keeps the rhythm), or click one note to re-roll it. You can set key, scale, groove, length, tempo, swing and melody range. Turn on "New pattern every loop" for an endless stream.

**Sound rack**
- **Sustain**: note length, release, vibrato, reverb, echo.
- **Layers**: harmony voice, a choir pad made from your own chops, and a simple drum beat.
- **Glitch**: chaos, speed jumps, stutter, reverse swells, rests.
- **Filter**: low-, high- or band-pass with resonance, per-note jitter and sweep.
- **Bitcrusher**: bit depth and downsampling on a chosen share of notes.
- **Motion**: slides between notes, bends, tape stops, stereo pan.

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
