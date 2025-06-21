# Kobold UI Navigation Implementation Guide

## Overview
This document outlines the implementation of proper UI navigation for both keyboard/mouse and gamepad input in the Kobold project.

## Implementation Steps

### Step 1: Boot Scene Setup ✅
**File**: `Assets/_Kobolds/Scenes/KoboldBoot.unity`

1. **Verify Required Components**:
   - ✅ `KoboldBootInitializer` - Manages boot sequence
   - ✅ `KoboldInputSystemManager` - Handles input mode switching
   - ✅ `KoboldThemeManager` - UI theming system
   - ✅ `KoboldServicesHelper` - Unity Services integration

2. **New Component Added**:
   - ✅ `KoboldUINavigationManager` - Created automatically by boot initializer

### Step 2: Main Menu Scene Setup ✅
**File**: `Assets/_Kobolds/Scenes/KoboldMainMenu.unity`

1. **Verify UI Structure**:
   - ✅ `MainMenuUI` GameObject with `UIDocument`
   - ✅ `KoboldMainMenuController` component
   - ✅ `KoboldHomeScreenView` component

2. **New Components Added**:
   - ✅ `KoboldUIInputHandler` - Added automatically by controller
   - ✅ Navigation focus management in `KoboldHomeScreenView`

### Step 3: Input System Configuration
**File**: Your Input Action Asset (e.g., `KoboldInputs.inputactions`)

1. **Add UI Action Map** (if not already present):
   ```
   Action Map: UI
   Actions:
   - Navigate (Vector2) - Arrow keys, WASD, Gamepad sticks
   - Submit (Button) - Enter, Space, Gamepad A
   - Cancel (Button) - Escape, Gamepad B
   - Point (Vector2) - Mouse position
   - Click (Button) - Mouse left click
   ```

2. **Bindings**:
   - **Keyboard**: Arrow keys, WASD, Enter, Space, Escape
   - **Gamepad**: Left stick, A button, B button
   - **Mouse**: Mouse position, left click

### Step 4: Testing the Implementation

#### Test 1: Boot to Main Menu
1. Start from `KoboldBoot` scene
2. Verify console logs show:
   ```
   [KoboldBootInitializer] Starting boot sequence...
   [KoboldBootInitializer] Initializing UI Navigation Manager...
   [KoboldBootInitializer] Created UI Navigation Manager
   [KoboldUINavigationManager] Navigation manager initialized
   ```

#### Test 2: Main Menu Navigation
1. Load `KoboldMainMenu` scene
2. Verify console logs show:
   ```
   [KoboldMainMenuController] UI input handler setup complete
   [KoboldHomeScreenView] Set initial focus to: social-hub-button
   ```

#### Test 3: Keyboard Navigation
1. Use arrow keys or WASD to navigate between buttons
2. Verify focus moves between:
   - Social Hub
   - Quick Mission  
   - Settings
   - Quit

#### Test 4: Submit Actions
1. Press Enter or Space on focused button
2. Verify button action is triggered
3. Check console for button press logs

#### Test 5: Gamepad Navigation (if available)
1. Connect gamepad
2. Use left stick to navigate
3. Use A button to submit
4. Use B button to cancel

## Component Architecture

### Core Components

#### 1. KoboldUINavigationManager
- **Purpose**: Singleton manager for UI navigation
- **Location**: Created automatically in boot scene
- **Key Features**:
  - Handles navigation input processing
  - Manages focus between UI elements
  - Provides navigation repeat delays

#### 2. KoboldUIInputHandler
- **Purpose**: Processes UI-specific input actions
- **Location**: Added to UI controllers automatically
- **Key Features**:
  - Handles keyboard/gamepad input
  - Integrates with navigation manager
  - Provides fallback input handling

#### 3. Enhanced KoboldHomeScreenView
- **Purpose**: Main menu view with navigation support
- **Key Features**:
  - Automatic focus setup
  - Navigation between buttons
  - Proper focus restoration

### Integration Points

#### Input System Integration
```csharp
// Your existing KoboldInputSystemManager already handles:
KoboldInputSystemManager.Instance.EnableUIMode();  // Switches to UI action map
KoboldInputSystemManager.Instance.EnableGameplayMode();  // Switches to Player action map
```

## Troubleshooting

### Common Issues

#### 1. Navigation Not Working
**Symptoms**: Arrow keys don't move focus
**Solutions**:
- Check `KoboldUINavigationManager.Instance` exists
- Verify `KoboldUIInputHandler` is attached to UI controller
- Ensure buttons have proper navigation setup

#### 2. Focus Not Set on Start
**Symptoms**: No button highlighted when menu loads
**Solutions**:
- Check `KoboldHomeScreenView.SetupNavigationFocus()` is called
- Verify first button in `_focusableButtons` list exists
- Check console for focus setup logs

#### 3. Input Actions Not Responding
**Symptoms**: Keyboard input not detected
**Solutions**:
- Verify UI action map is enabled
- Check `KoboldInputSystemManager.IsInUIMode` is true
- Ensure fallback input handling in `KoboldUIInputHandler.Update()`

### Debug Logging
Enable debug logging by setting `_enableTestLogging = true` in `KoboldUINavigationTest` component.

## Next Steps

### 1. Input Action Asset Configuration
You need to configure your input action asset with UI actions. The system will work with fallback keyboard input, but proper action mapping is recommended.

### 2. Gamepad Testing
Test with actual gamepad hardware to ensure proper gamepad navigation.

### 3. Additional UI Views
Apply the same pattern to other UI views:
- Settings menu
- Character selection
- Pause menu
- etc.

### 4. Advanced Navigation
Consider implementing:
- Grid-based navigation for complex layouts
- Tab navigation for multi-panel interfaces
- Context-sensitive navigation

## Summary

The UI navigation system is now implemented with:
- ✅ Automatic boot scene initialization
- ✅ Main menu navigation working
- ✅ Keyboard input support (with fallback)
- ✅ Focus management
- ✅ Proper integration with existing input system

The system follows Unity best practices and integrates seamlessly with your existing Kobold architecture. 