# IndustrialAR

IndustrialAR is a Unity mixed-reality prototype for industrial supervision, component recognition, real-time telemetry, and wiring assistance.

The project links a Unity/Vuforia scene to a remote Node-RED instance so an engineer can detect industrial components, view their dashboards in the camera scene, follow live values, inspect connection logs, and open a floating wiring guide.

## Features

- Vuforia-based image target detection for industrial components.
- World-space dashboards attached to detected components.
- Camera-facing dashboard behavior for readable MR overlays.
- Node-RED live connection using IP address, port, and password.
- Real-time telemetry widgets for PM2200, Siemens S7, VFD/motor, signal tower, HMI, and emergency stop.
- PM2200 values mapped for `Vab`, `Vbc`, `Vca`, and power factor.
- S7 and VFD frequency display.
- Compact MR tools panel, live telemetry, and connection log terminal.
- Floating wiring guide with transparent layout and surface-stick option.
- Unity editor validation tools for lab readiness and missing script checks.

## Tech Stack

- Unity `6000.4.3f1`
- C#
- Vuforia Engine `11.4.4`
- Node-RED
- HTTP/JSON telemetry
- UGUI / world-space Canvas
- Universal Render Pipeline
- Git LFS for the local Vuforia package tarball

## Project Structure

```text
Assets/
  Editor/             Unity editor tools and validators
  Scenes/             Main Unity scene
  Scripts/            Runtime logic, dashboards, Node-RED client, Vuforia handlers
  StreamingAssets/    Vuforia target database
NodeRED/              Node-RED flow export for the Unity bridge
Packages/             Unity package manifest and local Vuforia package
ProjectSettings/      Unity project settings
QCAR/                 Vuforia generated/support data
Tools/                Project support utilities
```

## Node-RED Integration

Default target instance:

```text
http://200.200.200.177:1880
```

Main endpoints used by the Unity client:

| Endpoint | Method | Purpose |
|---|---:|---|
| `/twin-data` | GET | Primary live telemetry |
| `/telemetry` | GET | Legacy/fallback telemetry |
| `/commands` | POST | Commands from Unity to Node-RED |
| `/state` | POST | Unity state published to Node-RED |
| `/ui` | GET | Node-RED dashboard reference |

Authentication is expected through `X-Node-RED-Password` or a Bearer token.

## Expected Telemetry Fields

```json
{
  "pm2200": {
    "Vab": 400.1,
    "Vbc": 399.8,
    "Vca": 401.0,
    "facteur puissance": 0.96
  },
  "s7": {
    "frequence": 50.0,
    "etat": "RUN"
  },
  "variateur": {
    "frequence": 42.5,
    "etat": "RUN"
  }
}
```

## Setup

1. Install Git LFS:

   ```bash
   git lfs install
   ```

2. Clone the repository:

   ```bash
   git clone https://github.com/med-reda-nk/IndustrialAR.git
   ```

3. Open the project with Unity `6000.4.3f1`.

4. Let Unity restore packages from `Packages/manifest.json`.

5. Open the main scene from `Assets/Scenes`.

6. In Play Mode, configure the Node-RED widget with the target IP address, port, and password.

## Lab Testing Checklist

- Node-RED is running on the remote machine.
- Unity machine can reach the Node-RED IP and port.
- `/twin-data` or `/telemetry` returns valid JSON.
- Vuforia targets are printed or displayed clearly.
- Camera permission is available.
- Unity Console has no blocking errors.
- Dashboards appear when targets are detected.
- Dashboards disappear when targets are lost.
- PM2200 values match Node-RED for `Vab`, `Vbc`, `Vca`, and power factor.
- Wiring guide opens, closes, and fits the camera view in stick mode.

## Notes

The project is a prototype for supervision, diagnostics, training, and lab validation. It is not a certified safety system and should not replace industrial SCADA, PLC safety logic, or machine safety validation.
