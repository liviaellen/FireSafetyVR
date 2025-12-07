# Don't Let it Spark XR

**An AI-Powered Fire Safety Training Game for Meta Quest**

Don't Let it Spark XR is an immersive VR training game that teaches fire safety through real-world object detection. Using your Quest's passthrough cameras and AI, the game helps you identify fire hazards in your actual environment.

## Game Modes

### 🔍 Fire Inspector Mode
**Role**: You are a building inspector conducting a safety assessment.

**Objective**: Identify and document fire hazards in your environment.

**Gameplay**:
1. Press **A** to start Inspector Mode
2. A 5-second tutorial appears explaining your role
3. Point your controller at objects around you
4. The AI automatically detects and classifies objects
5. Fire hazards appear with **red flame icons** 🔥
6. Safe objects appear with **blue ice icons** ❄️
7. Press **A** to mark objects for your assessment report

**Mechanics**:
- Real-time AI object detection using YOLO model
- Visual feedback with color-coded bounding boxes
- 3D markers placed in your physical space
- Persistent markers you can walk around and inspect

### 📚 Safety Quiz Mode
**Role**: You are a worker learning about fire safety.

**Objective**: Test your knowledge by identifying fire hazards correctly.

**Gameplay**:
1. Press **B** to start Quiz Mode
2. A 5-second tutorial explains the quiz rules
3. Point at an object and press **A** to select it
4. A question appears: "Is this [object] a Fire Hazard?"
5. Answer using gestures:
   - **Index Finger (Trigger/A)** = YES, it's a hazard
   - **Middle Finger (Grip/B)** = NO, it's safe
6. Get immediate feedback: "Correct!" or "Wrong!"
7. Complete 3 questions to see your final score

**Mechanics**:
- Objects shown with **neutral yellow boxes** (no spoilers!)
- 0.5-second input delay prevents accidental answers
- Score displayed as percentage (0-100%)
- Learn from mistakes with instant feedback

## Fire Hazard Classification

The AI identifies common household fire hazards:

**🔥 Fire Hazards** (Red):
- **Electronics**: Laptop, TV, Cell Phone, Microwave, Toaster, Oven
- **Reason**: Generate heat, have electrical components, can overheat

**❄️ Safe Objects** (Blue):
- **People & Furniture**: Person, Chair, Couch, Bed, Dining Table
- **Non-Electronics**: Book, Vase, Potted Plant, Clock
- **Reason**: No heat generation, non-flammable or low fire risk

## Controls

| Action | Input |
|--------|-------|
| Start Fire Inspector | **A Button** or **Index Trigger** |
| Start Safety Quiz | **B Button** or **Grip Trigger** |
| Select Object | **A Button** or **Index Trigger** |
| Answer "YES" (Quiz) | **Index Finger (Trigger)** |
| Answer "NO" (Quiz) | **Middle Finger (Grip)** |
| Return to Menu | **Menu Button** |

## Requirements

- **Hardware**: Meta Quest 3 or Quest 3S
- **OS**: Horizon OS v74 or higher
- **Permissions**: Camera access (granted on first launch)
- **Space**: 2m x 2m play area recommended

## Installation

1. Download the APK to your Quest
2. Enable Developer Mode and install via SideQuest or ADB
3. Launch "Don't Let it Spark XR"
4. Grant camera permissions when prompted
5. Choose your mode and start learning!

## Educational Value

**Don't Let it Spark XR** teaches fire safety through:
- **Active Learning**: Hands-on interaction with real objects
- **Immediate Feedback**: Learn from mistakes in real-time
- **Contextual Training**: Practice in your actual environment
- **Gamification**: Quiz scoring motivates learning
- **Visual Memory**: Associate objects with fire risk levels

Perfect for:
- Fire safety training programs
- Building inspector certification
- Workplace safety education
- Home fire prevention awareness

## Technical Details

- **Platform**: Unity 6000.0.38f1
- **XR SDK**: Meta XR with Passthrough Camera API
- **AI Model**: YOLO v8 via Unity Sentis
- **Inference**: Real-time on-device processing
- **Performance**: 30+ FPS on Quest 3/3S

## Credits

Built with Unity's Passthrough Camera API and Unity Sentis for on-device AI inference.

---

**Stay Safe. Don't Let it Spark.** 🔥❄️
