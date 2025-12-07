# Don't Let it Spark XR

**Fire Safety Training Application for Meta Quest**

An interactive VR fire safety training application that uses real-time AI object detection to help building inspectors and workers identify fire hazards in their actual environment.

## Features

### 🔍 Fire Inspector Mode (Press A)
- Real-time object detection using AI
- Automatic fire hazard identification
- Visual markers for hazardous and safe objects
- Document your safety assessments

### 📚 Safety Quiz Mode (Press B)
- Test your fire safety knowledge
- Interactive quiz with real objects
- Immediate feedback on answers
- Percentage-based scoring

## Requirements

- **Hardware:** Meta Quest 3 / Quest 3S
- **OS:** Horizon OS v74 or higher
- **Unity:** 6000.0.38f1 or newer
- **Permissions:** Camera access required

## Controls

- **A Button / Index Trigger**: Fire Inspector Mode or select objects
- **B Button / Grip Trigger**: Safety Quiz Mode or answer "No"
- **Menu Button**: Return to main menu

## How to Use

1. Launch the app on your Quest headset
2. Grant camera permissions when prompted
3. Choose a mode:
   - **Fire Inspector (A)**: Scan your environment for fire hazards
   - **Safety Quiz (B)**: Test your knowledge with a 3-question quiz
4. Follow the on-screen instructions

## Fire Hazard Classification

Objects are classified based on fire risk:
- **Hazardous**: Electronics (laptops, ovens, microwaves, toasters, TVs, phones)
- **Safe**: People, furniture, books, non-electronic items

## Technical Stack

- **Platform**: Unity 6000.0.38f1
- **XR Framework**: Meta XR SDK with Passthrough Camera API
- **AI Model**: YOLO object detection via Unity Sentis
- **Performance**: Real-time on-device inference

## License

This project is built on Unity's Passthrough Camera API Samples. See LICENSE files for details.

---

**Built with Unity • Powered by Unity Sentis • Designed for Meta Quest**
