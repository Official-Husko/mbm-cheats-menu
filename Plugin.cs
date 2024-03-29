using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using CustomScripts;
using MBMScripts;

namespace mbm_cheats_menu
{
    [BepInPlugin("husko.monsterblackmarket.cheats", "Monster Black Market Cheats", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private enum Tab
        {
            MainCheats,
            SpecialCheats,
            InteractiveSpots
        }

        private Tab _currentTab = Tab.MainCheats;
        private bool _showMenu;
        private Rect _menuRect = new(20, 20, 330, 240); // Initial position and size of the menu
        
        // Define separate arrays to store activation status for each tab
        private readonly bool[] _mainCheatsActivated = new bool[8];
        private readonly bool[] _specialCheatsActivated = new bool[2]; // Adjust the size as per your requirement
        private readonly bool[] _interactiveSpotsActivated = new bool[2];
        
        // Default max values
        private string addGoldAmountText = "0";
        private string addPixyAmountText = "0";

        
        private const string VersionLabel = MyPluginInfo.PLUGIN_VERSION;

        // List to store button labels and corresponding actions for the current cheats tab
        private readonly List<(string label, Action action)> _mainCheatsButtonActions = new()
        {
            // Add more buttons and actions here
        };

        // Modify the ghostModeButtonActions list to include a button for Special Cheats
        private readonly List<(string label, Action action)> _specialCheatsButtonActions = new()
        {
            // Add more buttons for Special Cheats here
        };
        
        /// <summary>
        /// Initializes the plugin on Awake event
        /// </summary>
        private void Awake()
        {
            // Log the plugin's version number and successful startup
            Logger.LogInfo($"Plugin mbm-cheats-menu v{VersionLabel} loaded!");
        }

        /// <summary>
        /// Handles toggling the menu on and off with the Insert or F1 key.
        /// </summary>
        private void Update()
        {
            // Toggle menu visibility with Insert or F1 key
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F1))
            {
                _showMenu = !_showMenu;
            }
        }

        /// <summary>
        /// Handles drawing the menu and all of its elements on the screen.
        /// </summary>
        private void OnGUI()
        {
            // Only draw the menu if it's supposed to be shown
            if (_showMenu)
            {
                // Apply dark mode GUI style
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

                // Draw the IMGUI window
                _menuRect = GUI.Window(0, _menuRect, MenuWindow, "----< Cheats Menu >----");

                // Calculate position for version label at bottom left corner
                float versionLabelX = _menuRect.xMin + 10; // 10 pixels from left edge
                float versionLabelY = _menuRect.yMax - 20; // 20 pixels from bottom edge

                // Draw version label at bottom left corner
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f); // Dark grey silver color
                GUI.Label(new Rect(versionLabelX, versionLabelY, 100, 20), "v" + VersionLabel);

                // Calculate the width of the author label
                float authorLabelWidth = GUI.skin.label.CalcSize(new GUIContent("by Official-Husko")).x + 10; // Add some extra width for padding

                // Calculate position for author label at bottom right corner
                float authorLabelX = _menuRect.xMax - authorLabelWidth; // 10 pixels from right edge
                float authorLabelY = versionLabelY + 2; // Align with version label

                // Draw the author label as a clickable label
                if (GUI.Button(new Rect(authorLabelX, authorLabelY, authorLabelWidth, 20), "<color=cyan>by</color> <color=yellow>Official-Husko</color>", GUIStyle.none))
                {
                    // Open a link in the user's browser when the label is clicked
                    Application.OpenURL("https://github.com/Official-Husko/Churn-Vector-Cheats");
                }
            }
        }

        /// <summary>
        /// Handles the GUI for the main menu
        /// </summary>
        /// <param name="windowID">The ID of the window</param>
        private void MenuWindow(int windowID)
        {
            // Make the whole window draggable
            GUI.DragWindow(new Rect(0, 0, _menuRect.width, 20));

            // Begin a vertical group for menu elements
            GUILayout.BeginVertical();

            // Draw tabs
            GUILayout.BeginHorizontal();
            // Draw the Main Cheats tab button
            DrawTabButton(Tab.MainCheats, "Main Cheats");
            // Draw the Special Cheats tab button
            DrawTabButton(Tab.SpecialCheats, "Special Cheats");
            // Draw Interactive Spots tab button
            DrawTabButton(Tab.InteractiveSpots, "Interactive Spots");
            GUILayout.EndHorizontal();

            // Draw content based on the selected tab
            switch (_currentTab)
            {
                // Draw the Main Cheats tab
                case Tab.MainCheats:
                    DrawMainCheatsTab();
                    break;
                // Draw the Special Cheats tab
                case Tab.SpecialCheats:
                    DrawSpecialCheatsTab();
                    break;
                // Draw the Interactive Spots tab
                case Tab.InteractiveSpots:
                    DrawInteractiveSpotsTab();
                    break;
            }

            // End the vertical group
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a tab button
        /// </summary>
        /// <param name="tab">The tab to draw</param>
        /// <param name="label">The label to display on the button</param>
        private void DrawTabButton(Tab tab, string label)
        {
            // Change background color based on the selected tab
            GUI.backgroundColor = _currentTab == tab ? Color.grey : Color.white;

            // If the button is clicked, set the current tab to the clicked tab
            if (GUILayout.Button(label))
            {
                _currentTab = tab;
            }
        }
        
        /// <summary>
        /// Gets the activation status array for the currently selected tab
        /// </summary>
        /// <returns>The activation status array for the current tab. If the tab is not recognized, null is returned.</returns>
        private bool[] GetCurrentTabActivationArray()
        {
            switch (_currentTab)
            {
                case Tab.MainCheats:
                    // Return the activation status array for the main cheats tab
                    return _mainCheatsActivated;
                case Tab.SpecialCheats:
                    // Return the activation status array for the special cheats tab
                    return _specialCheatsActivated;
                case Tab.InteractiveSpots:
                    // Return the activation status array for the interactive spots tab
                    return _interactiveSpotsActivated;
                default:
                    // If the tab is not recognized, return null
                    return null;
            }
        }
        
        /// <summary>
        /// Toggles the activation state of the button at the given index on the currently selected tab.
        /// If the index is not within the range of the activation status array for the current tab, nothing is done.
        /// </summary>
        /// <param name="buttonIndex">The index of the button to toggle activation status for</param>
        private void ToggleButtonActivation(int buttonIndex)
        {
            // Get the activation status array for the current tab. If the tab is not recognized, return.
            bool[] currentTabActivationArray = GetCurrentTabActivationArray();
            if (currentTabActivationArray == null)
            {
                return;
            }

            // If the index is within the range of the activation status array, toggle the activation status
            if (buttonIndex >= 0 && buttonIndex < currentTabActivationArray.Length)
            {
                currentTabActivationArray[buttonIndex] = !currentTabActivationArray[buttonIndex];
            }
        }

        /// <summary>
        /// Method to draw content for the Main Cheats tab
        /// </summary>
        private void DrawMainCheatsTab()
        {
            GUILayout.BeginVertical();

            // Draw buttons from the list
            for (int i = 0; i < _mainCheatsButtonActions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                DrawActivationDot(_mainCheatsActivated[i]); // Draw activation dot based on activation status
                
                // Draws a button for each cheat with the label, 
                // activation status, and invokes the action associated 
                // with the button when pressed
                if (GUILayout.Button(_mainCheatsButtonActions[i].label))
                {
                    ToggleButtonActivation(i); // Toggle activation status
                    _mainCheatsButtonActions[i].action.Invoke(); // Invoke the action associated with the button
                }
                GUILayout.EndHorizontal();
            }
            
            // Draw Gold option
            DrawAddGoldOption();
            
            // Draw Pixy option
            DrawAddPixyOption();
            
            // Draw Achievement Points option
            DrawAddAchievementPointsOption();
            
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the Special Cheats tab in the mod's UI
        /// </summary>
        private void DrawSpecialCheatsTab()
        {
            // Begin vertical layout for the tab
            GUILayout.BeginVertical();

            // Iterate through the list of special cheat buttons
            for (int i = 0; i < _specialCheatsButtonActions.Count; i++)
            {
                // Begin horizontal layout for the button row
                GUILayout.BeginHorizontal();

                // Draw an activation dot based on the activation status
                DrawActivationDot(_specialCheatsActivated[i]);

                // Draw a button for the special cheat
                if (GUILayout.Button(_specialCheatsButtonActions[i].label))
                {
                    // Toggle the activation status of the button
                    ToggleButtonActivation(i);

                    // Invoke the action associated with the button
                    _specialCheatsButtonActions[i].action.Invoke();
                }

                // End the horizontal layout for the button row
                GUILayout.EndHorizontal();
            }
            // End the vertical layout for the tab
            GUILayout.EndVertical();
        }
        
        // Draws the Interactive Spots tab in the mod's UI
        private void DrawInteractiveSpotsTab()
        {
        }


        /// <summary>
        /// Handles button click for toggling doors in the scene
        /// </summary>
        private static void GiveCoins()
        {
            // Debug log the action being performed
            Debug.Log("Give Coins");
        }

        /// <summary>
        /// Draws a small dot with a green color if the activation status is true, and red if it's false.
        /// This method uses the current tab activation status array to determine the dot color.
        /// </summary>
        /// <param name="activated">The activation status to determine the dot color.</param>
        private void DrawActivationDot(bool activated)
        {
            GetCurrentTabActivationArray(); // Consider current tab activation status array
            GUILayout.Space(10); // Add some space to center the dot vertically
            Color dotColor = activated ? Color.green : Color.red; // Determine dot color based on activation status
            GUIStyle dotStyle = new GUIStyle(GUI.skin.label); // Create a new GUIStyle for the dot label
            dotStyle.normal.textColor = dotColor; // Set the color of the dot label
            GUILayout.Label("●", dotStyle, GUILayout.Width(20), GUILayout.Height(20)); // Draw dot with the specified style
        }
        
        /// <summary>
        /// Draws the Plushy Uses option in the mod menu
        /// </summary>
        private void DrawAddGoldOption()
        {
            // Begin horizontal layout for the Plushy Uses option
            GUILayout.BeginHorizontal();

            // Load the gold icon from resources
            Sprite goldIcon = Resources.Load<Sprite>("icon_resource_gold_32px");
            
            // Draw the icon
            GUILayout.Label(new GUIContent(goldIcon.texture), GUILayout.Width(20), GUILayout.Height(20)); // Adjust width and height as needed
            
            // Add a label for the text field
            GUILayout.Label("Add Gold:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            addGoldAmountText = GUILayout.TextField(addGoldAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addGoldAmountInt;
            if (int.TryParse(addGoldAmountText, out addGoldAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addGoldAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.Gold += addGoldAmountInt;
                    }
                }
            }

            // End horizontal layout for the Plushy Uses option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Pixy option in the mod menu
        /// </summary>
        private void DrawAddPixyOption()
        {
            // Begin horizontal layout for the Add Pixy option
            GUILayout.BeginHorizontal();

            // Add a label for the text field
            GUILayout.Label("Add Pixy:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            addPixyAmountText = GUILayout.TextField(addPixyAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addPixyAmountInt;
            if (int.TryParse(addPixyAmountText, out addPixyAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addPixyAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.FloraPixyCount += addPixyAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Pixy option
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the Add Pixy option in the mod menu
        /// </summary>
        private void DrawAddAchievementPointsOption()
        {
            // Begin horizontal layout for the Add Pixy option
            GUILayout.BeginHorizontal();

            // Add a label for the text field
            GUILayout.Label("Add Achievement Points:"); // The text that appears next to the text field

            // Draw the text field and capture user input
            addPixyAmountText = GUILayout.TextField(addPixyAmountText, GUILayout.Width(40)); // The text field that the user can edit

            // Try to parse the input text as an integer
            int addPixyAmountInt;
            if (int.TryParse(addPixyAmountText, out addPixyAmountInt))
            {
                // Check if the parsed integer value is greater than 0
                if (addPixyAmountInt > 0)
                {
                    // Draw the add button with custom width and height
                    if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        GameManager.Instance.PlayerData.FloraPixyCount += addPixyAmountInt;
                    }
                }
            }

            // End horizontal layout for the Add Pixy option
            GUILayout.EndHorizontal();
        }
    }
}